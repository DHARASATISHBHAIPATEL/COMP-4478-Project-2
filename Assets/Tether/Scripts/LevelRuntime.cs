using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tether
{
    /// <summary>
    /// Builds a LevelPlan into GameObjects and runs it: the Shift between eras,
    /// checkpoints and respawn, story cues, the Lantern core pulse and the cave-in chase.
    /// Each era lives under its own root object; a Shift turns one root off and the other on.
    /// </summary>
    public class LevelRuntime : MonoBehaviour
    {
        public static LevelRuntime I;

        public LevelPlan P;
        public bool InA = true;
        public int Charges;
        public bool Busy, Finished, ShiftLocked;
        public Player player;
        public readonly List<Collider2D> LadderTops = new List<Collider2D>();

        Transform common, rootA, rootB;
        readonly List<IResettable> resettables = new List<IResettable>();
        readonly List<Rect> shadows = new List<Rect>();
        readonly Dictionary<string, Npc> npcs = new Dictionary<string, Npc>();
        readonly List<Ent> cues = new List<Ent>();
        readonly HashSet<string> fired = new HashSet<string>();
        Vector2 respawn;
        bool respawnA, dying;
        float exitX = -1, pulseT;
        bool pulseWarned;
        CaveWall wall;
        PhysicsMaterial2D noFriction;

        public Era Current { get { return InA ? P.EraA : P.EraB; } }
        public Era Other { get { return InA ? P.EraB : P.EraA; } }

        public static LevelRuntime Build(LevelPlan p)
        {
            var go = new GameObject("Level: " + p.Title);
            var L = go.AddComponent<LevelRuntime>();
            L.Setup(p);
            return L;
        }

        void OnDestroy() { if (I == this) I = null; }

        void Setup(LevelPlan p)
        {
            I = this;
            P = p;
            Charges = p.Charges;
            InA = p.StartInA;
            noFriction = new PhysicsMaterial2D("LevelNoFriction") { friction = 0f };
            common = Child("Common");
            rootA = Child("Era A " + p.EraA);
            rootB = Child("Era B " + p.EraB);

            BuildTiles(1, rootA, p.EraA, rootB);
            BuildTiles(2, rootB, p.EraB, rootA);
            BuildLadders();
            BuildEnts();

            // invisible side walls
            Wall(-0.5f, p.H / 2f, 1, p.H + 30);
            Wall(p.W + 0.5f, p.H / 2f, 1, p.H + 30);

            player = Player.Create(p.Start);
            player.transform.SetParent(transform, true);
            respawn = p.Start;
            respawnA = InA;

            if (p.Chase)
            {
                var wgo = new GameObject("CaveIn");
                wgo.transform.SetParent(transform, false);
                wall = wgo.AddComponent<CaveWall>();
                wall.Init(p.H);
                wall.Place(-30);
            }

            ApplyEra(false);
            Cam.I.SetBounds(new Rect(0, 0, p.W, p.H));
            Cam.I.Follow(player.transform, true);
            Backdrop.I.Set(Current, p.Underground);
            bool two = p.ShiftEnabled && p.MusicB != null && p.MusicB != p.MusicA;
            AudioDir.I.PlayMusic(p.MusicA, two ? p.MusicB : null, InA || !two);
        }

        Transform Child(string n)
        {
            var t = new GameObject(n).transform;
            t.SetParent(transform, false);
            return t;
        }

        void Wall(float x, float y, float w, float h)
        {
            var go = new GameObject("Bound");
            go.transform.SetParent(common, false);
            go.transform.position = new Vector3(x, y);
            var b = go.AddComponent<BoxCollider2D>();
            b.size = new Vector2(w, h);
            b.sharedMaterial = noFriction;
        }

        // ---------------- building ----------------

        float CellHash(int x, int y) { return Mathf.Repeat(Mathf.Sin(x * 12.9898f + y * 78.233f) * 43758.5453f, 1f); }

        void BuildTiles(int side, Transform root, Era era, Transform ghostRoot)
        {
            var S = P.Solid;
            for (int x = 0; x < P.W; x++)
                for (int y = 0; y < P.H; y++)
                {
                    int m = S[x, y];
                    if ((m & side) == 0) continue;
                    bool top = y + 1 >= P.H || (S[x, y + 1] & side) == 0;
                    int variant = (int)(CellHash(x, y) * 4);
                    var go = new GameObject("t");
                    go.transform.SetParent(root, false);
                    go.transform.position = new Vector3(x + 0.5f, y + 0.5f);
                    Art.MakeRenderer(go, Art.Tile(era, top, variant, P.Underground), 0);
                    if (m != 3)
                    {
                        // faint ghost of this era's exclusive tiles, seen from the other era
                        var g = new GameObject("ghost");
                        g.transform.SetParent(ghostRoot, false);
                        g.transform.position = go.transform.position;
                        Art.MakeRenderer(g, Art.Tile(era, top, variant, P.Underground), -1, new Color(1f, 1f, 1f, 0.16f));
                    }
                }

            // greedy rectangles for colliders
            var used = new bool[P.W, P.H];
            for (int y = 0; y < P.H; y++)
                for (int x = 0; x < P.W; x++)
                {
                    if (used[x, y] || (S[x, y] & side) == 0) continue;
                    int w = 1;
                    while (x + w < P.W && !used[x + w, y] && (S[x + w, y] & side) != 0) w++;
                    int h = 1;
                    bool ok = true;
                    while (ok && y + h < P.H)
                    {
                        for (int i = x; i < x + w; i++)
                            if (used[i, y + h] || (S[i, y + h] & side) == 0) { ok = false; break; }
                        if (ok) h++;
                    }
                    for (int i = x; i < x + w; i++)
                        for (int j = y; j < y + h; j++) used[i, j] = true;
                    var c = new GameObject("col");
                    c.transform.SetParent(root, false);
                    c.transform.position = new Vector3(x + w / 2f, y + h / 2f);
                    var b = c.AddComponent<BoxCollider2D>();
                    b.size = new Vector2(w, h);
                    b.sharedMaterial = noFriction;
                }
        }

        void BuildLadders()
        {
            for (int x = 0; x < P.W; x++)
                for (int y = 0; y < P.H; y++)
                {
                    int m = P.Ladders[x, y];
                    if (m == 0) continue;
                    for (int side = 1; side <= 2; side++)
                    {
                        Transform root = side == 1 ? rootA : rootB;
                        Era era = side == 1 ? P.EraA : P.EraB;
                        bool here = (m & side) != 0;
                        var go = new GameObject("ladder");
                        go.transform.SetParent(root, false);
                        go.transform.position = new Vector3(x + 0.5f, y + 0.5f);
                        if (here) Art.MakeRenderer(go, Art.Ladder(era), 2);
                        else Art.MakeRenderer(go, Art.Ladder(side == 1 ? P.EraB : P.EraA), 1, new Color(1, 1, 1, 0.16f));
                        bool isTop = y + 1 >= P.H || (P.Ladders[x, y + 1] & side) == 0;
                        if (here && isTop)
                        {
                            var t = new GameObject("ladderTop");
                            t.transform.SetParent(root, false);
                            t.transform.position = new Vector3(x + 0.5f, y + 1 - 0.05f);
                            var b = t.AddComponent<BoxCollider2D>();
                            b.size = new Vector2(1f, 0.1f);
                            b.usedByEffector = true;
                            var eff = t.AddComponent<PlatformEffector2D>();
                            eff.useOneWay = true;
                            eff.surfaceArc = 160f;
                            LadderTops.Add(b);
                        }
                    }
                }
        }

        Transform RootFor(EM m) { return m == EM.Both ? common : m == EM.A ? rootA : rootB; }

        GameObject Spawn(string n, Transform parent, float x, float y)
        {
            var go = new GameObject(n);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, y);
            return go;
        }

        void BuildEnts()
        {
            foreach (var e in P.Ents)
            {
                Transform parent = RootFor(e.M);
                Era era = e.M == EM.B ? P.EraB : P.EraA;
                switch (e.K)
                {
                    case EK.Hollow:
                        { var h = Spawn("Hollow", parent, e.X, e.Y).AddComponent<Hollow>(); h.Init(e); resettables.Add(h); }
                        break;
                    case EK.Guard:
                        { var g = Spawn("Guard", parent, e.X, e.Y).AddComponent<Guard>(); g.Init(e, era); resettables.Add(g); }
                        break;
                    case EK.Spikes:
                        for (int side = 1; side <= 2; side++)
                            if (((int)e.M & side) != 0)
                                Spawn("Spikes", side == 1 ? rootA : rootB, e.X, e.Y).AddComponent<Spikes>().Init(e, side == 1 ? P.EraA : P.EraB);
                        break;
                    case EK.Crumble:
                        for (int side = 1; side <= 2; side++)
                        {
                            var c = Spawn("Crumble", side == 1 ? rootA : rootB, e.X + 0.5f, e.Y + 0.5f).AddComponent<Crumble>();
                            c.Init(Art.Tile(side == 1 ? P.EraA : P.EraB, true, 3, P.Underground));
                            resettables.Add(c);
                        }
                        break;
                    case EK.Gas:
                        Spawn("Gas", parent, e.X, e.Y).AddComponent<Gas>().Init(e);
                        break;
                    case EK.Shadow:
                        {
                            shadows.Add(new Rect(e.X, e.Y, e.W, e.H));
                            var s = Spawn("Shadow", common, e.X + e.W / 2f, e.Y + e.H / 2f);
                            s.transform.localScale = new Vector3(e.W, e.H, 1);
                            Art.MakeRenderer(s, Art.Pixel, 60, new Color(0.02f, 0.02f, 0.06f, 0.55f));
                        }
                        break;
                    case EK.Checkpoint:
                        Spawn("Checkpoint", common, e.X, e.Y).AddComponent<CheckpointMarker>().Init();
                        break;
                    case EK.Text:
                        {
                            var tz = Spawn("Text", common, e.X, e.Y).AddComponent<TextZone>();
                            tz.E = e;
                            if (e.Visual)
                            {
                                if (!string.IsNullOrEmpty(e.TextA)) Poster(rootA, e, P.EraA);
                                if (!string.IsNullOrEmpty(e.TextB)) Poster(rootB, e, P.EraB);
                            }
                        }
                        break;
                    case EK.Npc:
                        {
                            var n = Spawn("Npc " + e.Id, parent, e.X, e.Y).AddComponent<Npc>();
                            n.Init(e);
                            npcs[e.Id] = n;
                        }
                        break;
                    case EK.Cue: cues.Add(e); break;
                    case EK.Lamp: Spawn("Lamp", parent, e.X, e.Y).AddComponent<Lamp>().Init(); break;
                    case EK.Beam:
                        { var b = Spawn("Beam", parent, e.X, e.Y).AddComponent<Beam>(); b.Init(); resettables.Add(b); }
                        break;
                    case EK.Tether: Spawn("Tether", common, e.X, e.Y).AddComponent<TetherPickup>().Init(); break;
                    case EK.Exit: exitX = e.X; break;
                    case EK.Prop:
                        {
                            var go = Spawn("Prop " + e.Name, parent, e.X, e.Y);
                            go.transform.localScale = Vector3.one * e.Scale;
                            Art.MakeRenderer(go, Art.Prop(e.Name), e.Name == "glow" ? 3 : 4, e.Tint);
                        }
                        break;
                }
            }
        }

        void Poster(Transform root, Ent e, Era era)
        {
            var go = Spawn("Poster", root, e.X, e.Y + 1.1f);
            var p = Art.Pal(era);
            Color c = era == Era.Future ? new Color(0.75f, 0.62f, 0.55f) : era == Era.Modern ? new Color(0.6f, 0.85f, 1f) : era == Era.Past ? new Color(0.95f, 0.85f, 0.6f) : p.Accent;
            Art.MakeRenderer(go, Art.Prop("poster"), 3, c);
        }

        // ---------------- queries ----------------

        int Side { get { return InA ? 1 : 2; } }

        public bool SolidAt(float x, float y)
        {
            int cx = Mathf.FloorToInt(x), cy = Mathf.FloorToInt(y);
            if (cx < 0 || cx >= P.W) return true;
            if (cy < 0 || cy >= P.H) return false;
            return (P.Solid[cx, cy] & Side) != 0;
        }

        public bool LadderAt(float x, float y)
        {
            int cx = Mathf.FloorToInt(x), cy = Mathf.FloorToInt(y);
            if (cx < 0 || cx >= P.W || cy < 0 || cy >= P.H) return false;
            return (P.Ladders[cx, cy] & Side) != 0;
        }

        public bool LineClear(Vector2 a, Vector2 b)
        {
            float d = Vector2.Distance(a, b);
            int n = Mathf.CeilToInt(d / 0.25f);
            for (int i = 1; i < n; i++)
            {
                var p = Vector2.Lerp(a, b, i / (float)n);
                if (SolidAt(p.x, p.y)) return false;
            }
            return true;
        }

        public bool InShadow(Vector2 p)
        {
            foreach (var r in shadows) if (r.Contains(p)) return true;
            return false;
        }

        public Npc NpcById(string id) { npcs.TryGetValue(id, out var n); return n; }

        bool InPulseZone { get { return P.PulseX0 >= 0 && player.Pos.x >= P.PulseX0 && player.Pos.x <= P.PulseX1; } }

        // ---------------- eras ----------------

        void ApplyEra(bool fx)
        {
            rootA.gameObject.SetActive(InA);
            rootB.gameObject.SetActive(!InA);
            if (fx)
            {
                AudioDir.I.SetEra(InA);
                AudioDir.I.Play("shift", 0.7f);
                UI.I.Flash(0.45f);
                Cam.I.ZoomPulse();
                Backdrop.I.Set(Current, P.Underground);
            }
        }

        bool BlockedInOther()
        {
            int other = InA ? 2 : 1;
            Rect b = player.Bounds;
            int x0 = Mathf.FloorToInt(b.xMin + 0.06f), x1 = Mathf.FloorToInt(b.xMax - 0.06f);
            int y0 = Mathf.FloorToInt(b.yMin + 0.06f), y1 = Mathf.FloorToInt(b.yMax - 0.06f);
            for (int x = x0; x <= x1; x++)
                for (int y = y0; y <= y1; y++)
                {
                    if (x < 0 || y < 0 || x >= P.W || y >= P.H) continue;
                    if ((P.Solid[x, y] & other) != 0) return true;
                }
            return false;
        }

        public bool TryShift(bool manual)
        {
            if (Busy || dying || Finished) return false;
            string why = null;
            if (!P.ShiftEnabled) why = P.Charges == 0 ? "The Tether is dead." : "You have no way to Shift.";
            else if (ShiftLocked) why = "The Tether does not answer.";
            else if (manual && InPulseZone) why = "The core is pulling the Tether. Wait for the pulse.";
            else if (Charges == 0) why = "The Tether is silent. Find one of Otto's lamps.";
            else if (BlockedInOther()) why = "Something solid stands here in the other time.";
            if (why != null)
            {
                if (manual)
                {
                    AudioDir.I.Play("blocked", 0.7f);
                    Cam.I.Shake(0.15f);
                    UI.I.Toast(why);
                }
                return false;
            }
            if (Charges > 0) { Charges--; AudioDir.I.Play("charge", 0.6f); }
            InA = !InA;
            ApplyEra(true);
            return true;
        }

        /// <summary>Scripted era change used by cinematics (no charge cost, no checks).</summary>
        public void ForceEra(bool a, bool fx = true)
        {
            if (InA == a) return;
            InA = a;
            ApplyEra(fx);
        }

        // ---------------- life cycle ----------------

        public void SetCheckpoint(Vector2 p)
        {
            respawn = p + new Vector2(0, 0.05f);
            respawnA = InA;
        }

        public void Caught() { player.Frozen = true; }

        public void Kill()
        {
            if (dying || Finished || Busy) return;
            StartCoroutine(Die());
        }

        IEnumerator Die()
        {
            dying = true;
            player.Dead = true;
            player.Frozen = true;
            AudioDir.I.Play("hurt", 0.8f);
            Cam.I.Shake(0.35f);
            player.rb.simulated = false;
            yield return new WaitForSeconds(0.45f);
            yield return UI.I.FadeTo(1, 0.3f);
            foreach (var r in resettables) r.ResetState();
            player.rb.simulated = true;
            player.Teleport(respawn);
            player.Dead = false;
            player.Frozen = false;
            if (InA != respawnA) { InA = respawnA; ApplyEra(false); AudioDir.I.SetEra(InA); Backdrop.I.Set(Current, P.Underground); }
            pulseT = 0; pulseWarned = false;
            if (wall != null && wall.Active) wall.Place(respawn.x - 9f);
            Cam.I.Follow(player.transform, true);
            AudioDir.I.Play("rewind", 0.6f);
            yield return UI.I.FadeTo(0, 0.35f);
            dying = false;
        }

        public void StartChase()
        {
            if (wall == null) return;
            wall.Active = true;
            wall.Place(player.Pos.x - 9f);
            AudioDir.I.SetLoop("rumble", 0.35f);
        }

        public void StopChase()
        {
            if (wall != null) wall.Active = false;
            AudioDir.I.SetLoop(null, 0);
        }

        public void Finish() { Finished = true; }

        void Update()
        {
            if (player == null || (Game.I != null && Game.I.Paused)) return;
            UI.I.Hud(this);
            if (Busy || dying || Finished) return;

            if (player.Pos.y < -3f) { Kill(); return; }

            for (int i = 0; i < cues.Count; i++)
            {
                var c = cues[i];
                if (!fired.Contains(c.Id) && player.Pos.x >= c.X) { RunCue(c.Id, c); break; }
            }

            if (exitX >= 0 && player.Pos.x >= exitX) { Finished = true; return; }

            if (P.PulseX0 >= 0)
            {
                if (InPulseZone)
                {
                    pulseT += Time.deltaTime;
                    if (pulseT > P.PulseInterval - 0.8f && !pulseWarned)
                    {
                        pulseWarned = true;
                        AudioDir.I.Play("hum", 0.8f);
                        UI.I.Toast("The core is about to pulse...");
                    }
                    if (pulseT >= P.PulseInterval)
                    {
                        pulseT = 0; pulseWarned = false;
                        InA = !InA;
                        ApplyEra(true);
                        Cam.I.Shake(0.2f);
                    }
                }
                else { pulseT = 0; pulseWarned = false; }
            }
        }

        public void RunCue(string id, Ent e)
        {
            if (fired.Contains(id)) return;
            fired.Add(id);
            if (P.Cues.TryGetValue(id, out var script)) StartCoroutine(CueRoutine(script, e));
        }

        IEnumerator CueRoutine(System.Func<LevelRuntime, Ent, IEnumerator> script, Ent e)
        {
            Busy = true;
            player.Frozen = true;
            yield return script(this, e);
            if (player != null) { player.Frozen = false; player.PoseOverride = null; player.ScriptMoveX = 0; }
            Cam.I.Release();
            UI.I.LetterboxTarget = 0;
            Busy = false;
        }

        // ---------------- cinematic helpers ----------------

        public IEnumerator Say(string name, string text, Npc who = null)
        {
            if (who != null) who.Talking = true;
            yield return UI.I.Say(name, text);
            if (who != null) who.Talking = false;
        }

        public IEnumerator Wait(float s) { yield return new WaitForSeconds(s); }

        public IEnumerator Letterbox(bool on)
        {
            UI.I.LetterboxTarget = on ? 1 : 0;
            yield return new WaitForSeconds(0.5f);
        }

        public IEnumerator WalkTo(float x, float speed = 3f)
        {
            float guard = 8f;
            while (Mathf.Abs(player.Pos.x - x) > 0.15f && guard > 0)
            {
                player.ScriptMoveX = Mathf.Sign(x - player.Pos.x) * speed;
                guard -= Time.deltaTime;
                yield return null;
            }
            player.ScriptMoveX = 0;
        }
    }
}
