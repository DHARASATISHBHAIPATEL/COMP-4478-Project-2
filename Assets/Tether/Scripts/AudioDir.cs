using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Tether
{
    /// <summary>
    /// Plays music and sound effects. Two music sources play the two eras of a
    /// level in sync, and a Shift crossfades between them in 0.3 seconds.
    /// Music and SFX have separate volume channels set from Settings.
    /// </summary>
    public class AudioDir : MonoBehaviour
    {
        public static AudioDir I;

        AudioSource musA, musB, loop;
        readonly List<AudioSource> pool = new List<AudioSource>();
        int poolNext;
        float na, nb, targetA, targetB, master = 1f, masterTarget = 1f, loopTarget;
        string curA, curB;

        readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        readonly Dictionary<string, Task<float[]>> musicJobs = new Dictionary<string, Task<float[]>>();

        static readonly string[] MusicNames = { "theme", "future", "modern", "past", "cave", "chase", "epilogue" };

        void Awake()
        {
            I = this;
            musA = NewSource(true);
            musB = NewSource(true);
            loop = NewSource(true);
            for (int i = 0; i < 10; i++) pool.Add(NewSource(false));
            // Render all music on worker threads so the game does not hitch.
            foreach (var n in MusicNames)
            {
                string name = n;
                musicJobs[name] = Task.Run(() => Synth.Music(name));
            }
        }

        AudioSource NewSource(bool looping)
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = looping;
            s.spatialBlend = 0;
            return s;
        }

        AudioClip Music(string name)
        {
            if (clips.TryGetValue("m_" + name, out var c)) return c;
            float[] data = musicJobs.TryGetValue(name, out var job) ? job.Result : Synth.Music(name);
            c = AudioClip.Create("m_" + name, data.Length, 1, Synth.SR, false);
            c.SetData(data, 0);
            clips["m_" + name] = c;
            return c;
        }

        AudioClip Sfx(string name)
        {
            if (clips.TryGetValue(name, out var c)) return c;
            float[] data = Synth.Sfx(name);
            c = AudioClip.Create(name, data.Length, 1, Synth.SR, false);
            c.SetData(data, 0);
            clips[name] = c;
            return c;
        }

        /// <summary>Plays one track, or two era tracks in sync (b may be null).</summary>
        public void PlayMusic(string a, string b = null, bool aActive = true)
        {
            if (a == curA && b == curB && musA.isPlaying) { SetEra(aActive); return; }
            curA = a; curB = b;
            musA.clip = a != null ? Music(a) : null;
            musB.clip = b != null ? Music(b) : null;
            musA.timeSamples = 0;
            if (musA.clip != null) musA.Play(); else musA.Stop();
            if (musB.clip != null) { musB.Play(); musB.timeSamples = 0; } else musB.Stop();
            masterTarget = 1f;
            master = 0f;
            SetEra(aActive, true);
        }

        public void SetEra(bool aActive, bool instant = false)
        {
            targetA = aActive || musB.clip == null ? 1 : 0;
            targetB = musB.clip == null ? 0 : (aActive ? 0 : 1);
            if (instant) { na = targetA; nb = targetB; }
        }

        public void FadeOutMusic() { masterTarget = 0f; }

        public void StopMusic()
        {
            musA.Stop(); musB.Stop();
            curA = curB = null;
        }

        public void Play(string name, float vol = 1f, float pitch = 1f)
        {
            var s = pool[poolNext];
            poolNext = (poolNext + 1) % pool.Count;
            s.clip = Sfx(name);
            s.pitch = pitch;
            s.volume = vol * Settings.Sfx;
            s.Play();
        }

        public void SetLoop(string name, float vol)
        {
            if (name == null) { loopTarget = 0; return; }
            var c = Sfx(name);
            if (loop.clip != c) { loop.clip = c; loop.Play(); loop.volume = 0; }
            loopTarget = vol;
            if (!loop.isPlaying) loop.Play();
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            master = Mathf.MoveTowards(master, masterTarget, dt * 0.8f);
            float m = Settings.Music * master;
            float k = dt / 0.3f;
            na = Mathf.MoveTowards(na, targetA, k);
            nb = Mathf.MoveTowards(nb, targetB, k);
            // keep the two era tracks sample-locked
            if (musA.isPlaying && musB.isPlaying && Mathf.Abs(musA.timeSamples - musB.timeSamples) > 2048)
                musB.timeSamples = musA.timeSamples;
            musA.volume = na * m;
            musB.volume = nb * m;
            loop.volume = Mathf.MoveTowards(loop.volume, loopTarget * Settings.Sfx, dt);
            if (loop.volume <= 0.001f && loopTarget <= 0 && loop.isPlaying) loop.Stop();
        }
    }
}
