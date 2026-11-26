using System;

namespace Tether
{
    /// <summary>
    /// A tiny software synthesizer. All music and sound effects in the game are
    /// generated here at runtime, so every sound is original to this project.
    /// Pure C# (no Unity calls) so music can be rendered on a background thread.
    /// </summary>
    public static class Synth
    {
        public const int SR = 44100;
        public const float LoopSeconds = 24f;

        public enum W { Sine, Tri, Saw, Square, Organ }

        static readonly int LoopLen = (int)(LoopSeconds * SR);

        static void Add(float[] b, int idx, float v)
        {
            if (idx >= b.Length)
            {
                if (b.Length != LoopLen) return; // one-shot sounds are clipped, music loops wrap
                idx %= b.Length;
            }
            b[idx] += v;
        }

        static double Freq(int midi) { return 440.0 * Math.Pow(2, (midi - 69) / 12.0); }

        static float Wave(W w, double ph)
        {
            double p = ph - Math.Floor(ph);
            switch (w)
            {
                case W.Tri: return (float)(p < 0.5 ? 4 * p - 1 : 3 - 4 * p);
                case W.Saw: return (float)(2 * p - 1);
                case W.Square: return p < 0.5 ? 0.8f : -0.8f;
                case W.Organ: return (float)(Math.Sin(2 * Math.PI * p) * 0.6 + Math.Sin(4 * Math.PI * p) * 0.3 + Math.Sin(6 * Math.PI * p) * 0.15);
                default: return (float)Math.Sin(2 * Math.PI * p);
            }
        }

        // ---------- building blocks (write with wrap-around so loops are seamless) ----------

        static void Tone(float[] b, double t0, double dur, int midi, float amp, W w, double atk = 0.01, double rel = 0.2, double vib = 0)
        {
            double f = Freq(midi);
            int s0 = (int)(t0 * SR);
            int n = (int)((dur + rel) * SR);
            for (int i = 0; i < n; i++)
            {
                double t = (double)i / SR;
                double env = t < atk ? t / atk : (t < dur ? 1 : Math.Max(0, 1 - (t - dur) / rel));
                double ph = f * t + (vib > 0 ? 0.003 * Math.Sin(2 * Math.PI * 5.5 * t) * f * vib / 5.5 : 0);
                Add(b, s0 + i, Wave(w, ph) * (float)env * amp);
            }
        }

        static void Piano(float[] b, double t0, int midi, float amp, double decay = 2.5)
        {
            double f = Freq(midi);
            int s0 = (int)(t0 * SR);
            int n = (int)(decay * 1.6 * SR);
            for (int i = 0; i < n; i++)
            {
                double t = (double)i / SR;
                double env = Math.Exp(-t * 3.0 / decay) * Math.Min(1, t / 0.004);
                double s = Math.Sin(2 * Math.PI * f * t) + 0.45 * Math.Sin(4 * Math.PI * f * t) * Math.Exp(-t * 2)
                           + 0.2 * Math.Sin(6 * Math.PI * f * t) * Math.Exp(-t * 4);
                Add(b, s0 + i, (float)(s * env) * amp);
            }
        }

        static void Pluck(float[] b, double t0, int midi, float amp, Random rng, double dur = 1.2)
        {
            int period = Math.Max(2, (int)(SR / Freq(midi)));
            var ring = new float[period];
            for (int i = 0; i < period; i++) ring[i] = (float)(rng.NextDouble() * 2 - 1);
            int s0 = (int)(t0 * SR);
            int n = (int)(dur * SR);
            int idx = 0;
            for (int i = 0; i < n; i++)
            {
                int nxt = (idx + 1) % period;
                float v = ring[idx];
                ring[idx] = 0.497f * (ring[idx] + ring[nxt]);
                idx = nxt;
                double fade = i > n - 2000 ? (n - i) / 2000.0 : 1;
                Add(b, s0 + i, v * amp * (float)fade);
            }
        }

        static void Kick(float[] b, double t0, float amp)
        {
            int s0 = (int)(t0 * SR);
            int n = (int)(0.35 * SR);
            double ph = 0;
            for (int i = 0; i < n; i++)
            {
                double t = (double)i / SR;
                double f = 50 + 90 * Math.Exp(-t * 30);
                ph += f / SR;
                Add(b, s0 + i, (float)(Math.Sin(2 * Math.PI * ph) * Math.Exp(-t * 9)) * amp);
            }
        }

        static void Noise(float[] b, double t0, double dur, float amp, double decay, Random rng, bool high = false, double lp = 1)
        {
            int s0 = (int)(t0 * SR);
            int n = (int)(dur * SR);
            float last = 0, prev = 0;
            for (int i = 0; i < n; i++)
            {
                double t = (double)i / SR;
                float r = (float)(rng.NextDouble() * 2 - 1);
                float v;
                if (high) { v = r - prev; prev = r; }
                else { last += (float)lp * (r - last); v = last; }
                Add(b, s0 + i, v * (float)Math.Exp(-t * decay) * amp);
            }
        }

        static void Wind(float[] b, float amp, Random rng)
        {
            float last = 0;
            for (int i = 0; i < b.Length; i++)
            {
                double t = (double)i / SR;
                last += 0.02f * ((float)(rng.NextDouble() * 2 - 1) - last);
                double swell = 0.6 + 0.4 * Math.Sin(2 * Math.PI * t / LoopSeconds * 3) * Math.Sin(2 * Math.PI * t / LoopSeconds * 2);
                b[i] += last * amp * (float)swell * 6;
            }
        }

        static void Finish(float[] b, float peak = 0.85f)
        {
            float m = 0.0001f;
            for (int i = 0; i < b.Length; i++) m = Math.Max(m, Math.Abs(b[i]));
            float g = peak / m;
            for (int i = 0; i < b.Length; i++) b[i] = (float)Math.Tanh(b[i] * g * 1.1) ;
        }

        static void LowPass(float[] b, float a)
        {
            float y = 0;
            for (int pass = 0; pass < 2; pass++)
                for (int i = 0; i < b.Length; i++) { y += a * (b[i] - y); b[i] = y; }
        }

        // chord tables: root + intervals (minor / major triads)
        static int[] Tri(int root, bool minor) { return new[] { root, root + (minor ? 3 : 4), root + 7 }; }

        // ---------- music ----------

        public static float[] Music(string name)
        {
            var b = new float[(int)(LoopSeconds * SR)];
            var rng = new Random(name.GetHashCode());
            switch (name)
            {
                case "theme": Theme(b, rng, false); break;
                case "epilogue": Theme(b, rng, true); break;
                case "future": Future(b, rng); break;
                case "modern": Modern(b, rng); break;
                case "past": Past(b, rng); break;
                case "chase": Chase(b, rng); break;
                case "cave": Cave(b, rng); break;
            }
            Finish(b);
            return b;
        }

        static void Theme(float[] b, Random rng, bool major)
        {
            // 80 BPM, 8 bars of 4/4 = 24 s
            double beat = 0.75;
            int[][] chords = major
                ? new[] { Tri(60, false), Tri(55, false), Tri(57, true), Tri(53, false) }      // C G Am F
                : new[] { Tri(57, true), Tri(53, false), Tri(60, false), Tri(55, false) };     // Am F C G
            for (int bar = 0; bar < 8; bar++)
            {
                var c = chords[bar % 4];
                double t = bar * 4 * beat;
                Tone(b, t, 4 * beat, c[0] - 24, 0.10f, W.Sine, 0.3, 0.6);
                foreach (var n in c) Tone(b, t, 4 * beat, n - 12, 0.035f, W.Tri, 0.8, 1.0);
                int[] arp = { c[0], c[1], c[2], c[1] + 12, c[2], c[1], c[0] + 12, c[2] };
                for (int i = 0; i < 8; i++) Piano(b, t + i * beat / 2, arp[i], 0.07f, 1.6);
                for (int i = 0; i < 4; i++) Noise(b, t + i * beat, 0.03, major ? 0.0f : 0.05f, 200, rng, true);
                if (major) // gentle melody
                {
                    int[] mel = { 7, 9, 7, 4 };
                    Tone(b, t + beat, 2.5 * beat, c[0] + 12 + mel[bar % 4] - 7 + 7, 0.05f, W.Organ, 0.2, 0.5, 1);
                }
            }
        }

        static void Future(float[] b, Random rng)
        {
            double beat = 0.75; // 80 BPM
            int[][] chords = { Tri(57, true), Tri(53, false), Tri(50, true), Tri(52, false) }; // Am F Dm E
            Tone(b, 0, LoopSeconds, 33, 0.09f, W.Sine, 2, 0.5);
            Tone(b, 0, LoopSeconds, 40, 0.04f, W.Tri, 2, 0.5);
            for (int bar = 0; bar < 8; bar++)
            {
                var c = chords[bar % 4];
                double t = bar * 4 * beat;
                foreach (var n in c) Tone(b, t, 4 * beat, n - 12, 0.03f, W.Tri, 1.2, 1.5);
                for (int i = 0; i < 3; i++)
                    if (rng.NextDouble() < 0.7) Piano(b, t + (i * 1.5 + rng.Next(0, 2) * 0.5) * beat, c[rng.Next(3)] + 12, 0.05f, 3);
            }
            Wind(b, 0.05f, rng);
        }

        static void Modern(float[] b, Random rng)
        {
            double beat = 0.5; // 120 BPM, 12 bars
            int[][] chords = { Tri(60, false), Tri(55, false), Tri(57, true), Tri(53, false) }; // C G Am F
            var bass = new float[b.Length];
            for (int bar = 0; bar < 12; bar++)
            {
                var c = chords[bar % 4];
                double t = bar * 4 * beat;
                foreach (var n in c) Tone(b, t, 4 * beat, n, 0.025f, W.Sine, 0.2, 0.3);
                int[] arp = { c[0] + 12, c[1] + 12, c[2] + 12, c[1] + 12 };
                for (int i = 0; i < 16; i++) Tone(b, t + i * beat / 4, 0.07, arp[i % 4], 0.035f, W.Square, 0.003, 0.05);
                for (int i = 0; i < 8; i++) Tone(bass, t + i * beat / 2, 0.2, c[0] - 24, 0.12f, W.Saw, 0.005, 0.05);
                for (int i = 0; i < 4; i++)
                {
                    Kick(b, t + i * beat, 0.22f);
                    Noise(b, t + i * beat + beat / 2, 0.05, 0.05f, 90, rng, true);
                    if (i % 2 == 1) Noise(b, t + i * beat, 0.18, 0.09f, 22, rng, false, 0.5);
                }
            }
            LowPass(bass, 0.08f);
            for (int i = 0; i < b.Length; i++) b[i] += bass[i] * 2.2f;
        }

        static void Past(float[] b, Random rng)
        {
            double beat = 2.0 / 3.0; // 90 BPM, 3/4, 12 bars = 24 s
            int[][] chords = { Tri(55, false), Tri(52, true), Tri(48, false), Tri(50, false) }; // G Em C D
            int[] melody = { 71, 69, 67, 64, 67, 69, 71, 74, 72, 71, 69, 66 };
            for (int bar = 0; bar < 12; bar++)
            {
                var c = chords[bar % 4];
                double t = bar * 3 * beat;
                Pluck(b, t, c[0] - 12, 0.22f, rng, 1.5);
                Pluck(b, t + beat, c[1], 0.12f, rng);
                Pluck(b, t + beat + 0.02, c[2], 0.10f, rng);
                Pluck(b, t + 2 * beat, c[1], 0.12f, rng);
                Pluck(b, t + 2 * beat + 0.02, c[2], 0.10f, rng);
                if (bar >= 4) Tone(b, t, 2.6 * beat, melody[bar], 0.045f, W.Organ, 0.08, 0.3, 1);
                foreach (var n in c) Tone(b, t, 3 * beat, n, 0.012f, W.Saw, 0.2, 0.3, 1);
            }
        }

        static void Cave(float[] b, Random rng)
        {
            double beat = 0.75;
            Tone(b, 0, LoopSeconds, 31, 0.10f, W.Sine, 2, 0.5);
            for (int bar = 0; bar < 8; bar++)
            {
                double t = bar * 4 * beat;
                for (int i = 0; i < 2; i++)
                    if (rng.NextDouble() < 0.8) Pluck(b, t + i * 2 * beat + rng.Next(0, 3) * 0.25, new[] { 55, 58, 62, 50 }[rng.Next(4)], 0.10f, rng, 2.0);
                if (rng.NextDouble() < 0.5) Piano(b, t + 1.5 * beat, 74 + rng.Next(0, 3), 0.025f, 3);
            }
            Wind(b, 0.035f, rng);
        }

        static void Chase(float[] b, Random rng)
        {
            double beat = 0.375; // 160 BPM, 16 bars
            int[] roots = { 50, 46, 48, 45 }; // Dm Bb C A
            for (int bar = 0; bar < 16; bar++)
            {
                int r = roots[bar % 4];
                bool minor = bar % 4 == 0;
                var c = Tri(r, minor);
                double t = bar * 4 * beat;
                foreach (var n in c) Tone(b, t, 4 * beat, n, 0.03f, W.Saw, 0.05, 0.1, 1);
                for (int i = 0; i < 8; i++) Tone(b, t + i * beat / 2, 0.15, r - 12, 0.08f, W.Saw, 0.003, 0.04);
                for (int i = 0; i < 4; i++)
                {
                    Kick(b, t + i * beat, 0.25f);
                    if (i % 2 == 1) Noise(b, t + i * beat, 0.2, 0.12f, 20, rng, false, 0.6);
                    Noise(b, t + i * beat + beat / 2, 0.04, 0.05f, 100, rng, true);
                }
                if (bar % 2 == 1) { Kick(b, t + 3.5 * beat, 0.18f); Tone(b, t + 3.5 * beat, 0.15, r - 5, 0.08f, W.Sine, 0.002, 0.1); }
                Tone(b, t, 4 * beat, c[2] + 12 + (bar / 4), 0.025f, W.Saw, 0.3, 0.2, 1);
            }
        }

        // ---------- sound effects ----------

        public static float[] Sfx(string name)
        {
            var rng = new Random(name.GetHashCode());
            float[] b;
            switch (name)
            {
                case "jump": b = Sweep(0.14, 280, 620, W.Square, 0.25f); break;
                case "land": b = new float[(int)(0.12 * SR)]; Noise(b, 0, 0.12, 0.5f, 40, rng, false, 0.15); break;
                case "step": b = new float[(int)(0.05 * SR)]; Noise(b, 0, 0.05, 0.25f, 80, rng, false, 0.35); break;
                case "shift":
                    b = Sweep(0.55, 220, 1400, W.Sine, 0.35f);
                    for (int i = 0; i < 4; i++) Tone(b, i * 0.08, 0.02, 96 - i * 2, 0.15f, W.Sine, 0.001, 0.05);
                    break;
                case "blocked": b = new float[(int)(0.3 * SR)]; Tone(b, 0, 0.22, 33, 0.4f, W.Square, 0.005, 0.06); break;
                case "hurt": b = Sweep(0.3, 420, 90, W.Saw, 0.3f); break;
                case "rewind": b = Sweep(0.6, 1400, 200, W.Sine, 0.3f); break;
                case "checkpoint":
                    b = new float[(int)(1.2 * SR)];
                    Piano(b, 0, 81, 0.3f, 1.2); Piano(b, 0.09, 88, 0.25f, 1.2); break;
                case "tick": b = new float[(int)(0.025 * SR)]; Noise(b, 0, 0.025, 0.18f, 200, rng, true); break;
                case "move": b = new float[(int)(0.06 * SR)]; Tone(b, 0, 0.03, 79, 0.2f, W.Sine, 0.002, 0.03); break;
                case "confirm":
                    b = new float[(int)(0.25 * SR)];
                    Tone(b, 0, 0.06, 79, 0.2f, W.Sine, 0.002, 0.05); Tone(b, 0.06, 0.08, 86, 0.2f, W.Sine, 0.002, 0.08); break;
                case "back": b = new float[(int)(0.12 * SR)]; Tone(b, 0, 0.06, 72, 0.2f, W.Sine, 0.002, 0.05); break;
                case "alert":
                    b = new float[(int)(0.5 * SR)];
                    Tone(b, 0, 0.08, 88, 0.3f, W.Square, 0.002, 0.02); Tone(b, 0.1, 0.12, 93, 0.3f, W.Square, 0.002, 0.02);
                    Noise(b, 0.22, 0.25, 0.2f, 12, rng, true); break;
                case "groan":
                    b = new float[(int)(0.9 * SR)];
                    Tone(b, 0, 0.7, 34, 0.35f, W.Saw, 0.1, 0.2, 3); LowPass(b, 0.06f); break;
                case "hum": b = new float[(int)(1.4 * SR)]; Tone(b, 0, 1.1, 28, 0.5f, W.Organ, 0.1, 0.3); break;
                case "hiss": b = new float[(int)(0.9 * SR)]; Noise(b, 0, 0.9, 0.25f, 2.5, rng, true); break;
                case "crack":
                    b = new float[(int)(0.6 * SR)];
                    Noise(b, 0, 0.05, 0.8f, 30, rng, true); Noise(b, 0.04, 0.5, 0.5f, 8, rng, false, 0.2); break;
                case "rumble": b = new float[(int)(2.5 * SR)]; Noise(b, 0, 2.5, 0.9f, 0.6, rng, false, 0.02); break;
                case "charge": b = new float[(int)(0.1 * SR)]; Noise(b, 0, 0.03, 0.4f, 120, rng, true); Tone(b, 0, 0.03, 60, 0.2f, W.Square, 0.001, 0.02); break;
                case "refill":
                    b = new float[(int)(0.9 * SR)];
                    Piano(b, 0, 76, 0.25f, 0.8); Piano(b, 0.08, 81, 0.25f, 0.8); Piano(b, 0.16, 88, 0.25f, 0.8); break;
                case "pickup":
                    b = new float[(int)(1.5 * SR)];
                    for (int i = 0; i < 6; i++) Piano(b, i * 0.07, 72 + new[] { 0, 4, 7, 12, 16, 19 }[i], 0.2f, 1.2); break;
                case "kick": b = new float[(int)(0.4 * SR)]; Kick(b, 0, 0.7f); Noise(b, 0, 0.1, 0.4f, 30, rng, false, 0.3); break;
                case "flare": b = Sweep(1.2, 60, 900, W.Saw, 0.25f); LowPass(b, 0.2f); break;
                default: b = new float[64]; break;
            }
            float m = 0.0001f;
            foreach (var v in b) m = Math.Max(m, Math.Abs(v));
            if (m > 1f) for (int i = 0; i < b.Length; i++) b[i] /= m;
            return b;
        }

        static float[] Sweep(double dur, double f0, double f1, W w, float amp)
        {
            var b = new float[(int)(dur * SR)];
            double ph = 0;
            for (int i = 0; i < b.Length; i++)
            {
                double k = (double)i / b.Length;
                double f = f0 * Math.Pow(f1 / f0, k);
                ph += f / SR;
                double env = Math.Min(1, k * 30) * (1 - k);
                b[i] = Wave(w, ph) * (float)env * amp;
            }
            return b;
        }
    }
}
