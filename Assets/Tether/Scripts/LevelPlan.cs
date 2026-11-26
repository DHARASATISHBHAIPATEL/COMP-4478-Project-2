using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tether
{
    /// <summary>Which era layer something belongs to. Both = exists in either time.</summary>
    [Flags] public enum EM { A = 1, B = 2, Both = 3 }

    public enum EK { Hollow, Guard, Spikes, Crumble, Gas, Shadow, Checkpoint, Text, Npc, Cue, Lamp, Beam, Tether, Exit, Prop }

    public class Ent
    {
        public EK K;
        public float X, Y, W = 1, H = 1;
        public EM M = EM.Both;
        public float Min, Max, Phase, Radius = 2.5f;
        public string TextA, TextB, Id, Set, Name;
        public bool FaceLeft, Visual = true;
        public Color Tint = Color.white;
        public float Scale = 1f;
    }

    /// <summary>
    /// A level described with a small building API: solids, ladders and entities
    /// placed on a 1-unit grid, x to the right, y up, (0,0) = bottom-left cell.
    /// </summary>
    public class LevelPlan
    {
        public string Title, Chapter;
        public Era EraA, EraB;
        public int W, H;
        public bool ShiftEnabled = true;
        public int Charges = -1;          // -1 = unlimited Shifts
        public bool Underground;
        public float PulseX0 = -1, PulseX1 = -1, PulseInterval = 2.8f;
        public bool Chase;
        public float ChaseSpeed = 4.0f;
        public Vector2 Start;
        public bool StartInA = true;
        public string MusicA, MusicB;

        public byte[,] Solid;    // EM mask per cell
        public byte[,] Ladders;  // EM mask per cell
        public readonly List<Ent> Ents = new List<Ent>();

        public Func<LevelRuntime, IEnumerator> Intro, Outro;
        public readonly Dictionary<string, Func<LevelRuntime, Ent, IEnumerator>> Cues = new Dictionary<string, Func<LevelRuntime, Ent, IEnumerator>>();

        public LevelPlan(string title, string chapter, Era a, Era b, int w, int h)
        {
            Title = title; Chapter = chapter; EraA = a; EraB = b; W = w; H = h;
            Solid = new byte[w, h];
            Ladders = new byte[w, h];
        }

        bool In(int x, int y) { return x >= 0 && y >= 0 && x < W && y < H; }

        public LevelPlan Block(int x, int y, int w, int h, EM m = EM.Both)
        {
            for (int i = x; i < x + w; i++)
                for (int j = y; j < y + h; j++)
                    if (In(i, j)) Solid[i, j] = (byte)m;
            return this;
        }

        /// <summary>Solid ground from y = 0 up to (not including) top.</summary>
        public LevelPlan Ground(int x, int w, int top, EM m = EM.Both) { return Block(x, 0, w, top, m); }

        public LevelPlan Clear(int x, int y, int w, int h) { return Block(x, y, w, h, 0); }

        /// <summary>Ladder cells x, y .. y+h-1. The top surface is y+h.</summary>
        public LevelPlan Ladder(int x, int y, int h, EM m = EM.Both)
        {
            for (int j = y; j < y + h; j++) if (In(x, j)) Ladders[x, j] = (byte)m;
            return this;
        }

        public Ent Add(EK k, float x, float y, EM m = EM.Both)
        {
            var e = new Ent { K = k, X = x, Y = y, M = m };
            Ents.Add(e);
            return e;
        }

        public LevelPlan Hollow(float x, float y, float min, float max, EM m = EM.A) { var e = Add(EK.Hollow, x, y, m); e.Min = min; e.Max = max; return this; }
        public LevelPlan Guard(float x, float y, float min, float max, EM m = EM.A) { var e = Add(EK.Guard, x, y, m); e.Min = min; e.Max = max; return this; }
        public LevelPlan Spikes(int x, int y, int w, EM m = EM.Both) { var e = Add(EK.Spikes, x, y, m); e.W = w; return this; }
        public LevelPlan Crumble(int x, int y, int w) { for (int i = 0; i < w; i++) Add(EK.Crumble, x + i, y); return this; }
        public LevelPlan Gas(int x, int y, int w, int h, float phase, EM m = EM.A) { var e = Add(EK.Gas, x, y, m); e.W = w; e.H = h; e.Phase = phase; return this; }
        public LevelPlan Shadow(int x, int y, int w, int h) { var e = Add(EK.Shadow, x, y); e.W = w; e.H = h; return this; }
        public LevelPlan Checkpoint(float x, float y) { Add(EK.Checkpoint, x, y); return this; }
        public LevelPlan Lamp(float x, float y, EM m = EM.A) { Add(EK.Lamp, x, y, m); return this; }
        public LevelPlan Beam(float x, float y, EM m = EM.A) { Add(EK.Beam, x, y, m); return this; }
        public LevelPlan TetherPickup(float x, float y) { Add(EK.Tether, x, y); return this; }
        public LevelPlan Exit(float x, float y) { Add(EK.Exit, x, y); return this; }

        /// <summary>On-screen story text near a spot. Shows textA in era A and textB in era B.</summary>
        public LevelPlan Text(float x, float y, string textA, string textB = null, bool poster = true, float radius = 2.5f)
        {
            var e = Add(EK.Text, x, y);
            e.TextA = textA; e.TextB = textB; e.Visual = poster; e.Radius = radius;
            return this;
        }

        public LevelPlan Npc(string id, string set, string name, float x, float y, EM m, bool faceLeft = true, Color? tint = null)
        {
            var e = Add(EK.Npc, x, y, m);
            e.Id = id; e.Set = set; e.Name = name; e.FaceLeft = faceLeft; e.Tint = tint ?? Color.white;
            return this;
        }

        /// <summary>A cinematic that runs once when the player reaches x.</summary>
        public LevelPlan Cue(string id, float x, Func<LevelRuntime, Ent, IEnumerator> script)
        {
            var e = Add(EK.Cue, x, 0);
            e.Id = id;
            Cues[id] = script;
            return this;
        }

        public LevelPlan Prop(string sprite, float x, float y, EM m = EM.Both, float scale = 1f, Color? tint = null)
        {
            var e = Add(EK.Prop, x, y, m);
            e.Name = sprite; e.Scale = scale; e.Tint = tint ?? Color.white;
            return this;
        }

        public Ent Find(string id)
        {
            foreach (var e in Ents) if (e.Id == id) return e;
            return null;
        }
    }
}
