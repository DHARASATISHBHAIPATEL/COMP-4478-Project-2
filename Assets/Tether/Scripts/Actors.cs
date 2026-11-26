using UnityEngine;

namespace Tether
{
    public interface IResettable { void ResetState(); }

    static class Act
    {
        public static bool Halted { get { return LevelRuntime.I == null || Player.I == null || LevelRuntime.I.Busy || (Game.I != null && Game.I.Paused); } }
    }

    /// <summary>Hollows: people changed by the Bloom. They wander, and chase on sight.</summary>
    public class Hollow : MonoBehaviour, IResettable
    {
        float min, max, dir = -1, animT, groanT;
        bool chasing;
        Vector3 home;
        SpriteRenderer sr;

        public void Init(Ent e)
        {
            min = e.Min; max = e.Max; home = transform.position;
            sr = Art.MakeRenderer(gameObject, Art.Char("Zombie", "idle"), 40, new Color(0.78f, 0.9f, 0.82f));
            groanT = Random.Range(1f, 4f);
        }

        public void ResetState() { transform.position = home; chasing = false; dir = -1; }

        void Update()
        {
            if (Act.Halted) return;
            var L = LevelRuntime.I; var p = Player.I;
            float dt = Time.deltaTime;
            Vector3 pos = transform.position;
            Vector2 pc = p.Center;
            float dx = pc.x - pos.x, dy = pc.y - (pos.y + 0.6f);
            bool sees = !p.Dead && Mathf.Abs(dy) < 1.6f && Mathf.Abs(dx) < 6.5f &&
                        (Mathf.Sign(dx) == Mathf.Sign(dir) || Mathf.Abs(dx) < 2f) && !L.InShadow(pc);
            if (sees && !chasing) { chasing = true; AudioDir.I.Play("groan", 0.8f, 1.1f); }
            if (chasing && (Mathf.Abs(dx) > 10 || Mathf.Abs(dy) > 3 || p.Dead)) chasing = false;

            float spd = chasing ? 3.3f : 1.1f;
            if (chasing) dir = Mathf.Sign(dx);
            else { if (pos.x <= min) dir = 1; if (pos.x >= max) dir = -1; }
            float nx = pos.x + dir * spd * dt;
            bool ground = L.SolidAt(nx + dir * 0.35f, pos.y - 0.5f);
            bool wall = L.SolidAt(nx + dir * 0.35f, pos.y + 0.5f);
            bool inRange = chasing || (nx >= min - 0.05f && nx <= max + 0.05f);
            if (ground && !wall && inRange) pos.x = nx;
            else if (!chasing) dir = -dir;
            transform.position = pos;

            animT += dt * (chasing ? 1.8f : 1f);
            sr.sprite = Art.Char("Zombie", Mathf.Repeat(animT, 0.5f) < 0.25f ? "walk1" : "walk2");
            sr.flipX = dir < 0;

            var r = new Rect(pos.x - 0.3f, pos.y, 0.6f, 1.1f);
            if (!p.Dead && p.Bounds.Overlaps(r)) L.Kill();

            groanT -= dt;
            if (groanT <= 0)
            {
                groanT = Random.Range(3f, 6f);
                float d = Mathf.Abs(dx);
                if (d < 9) AudioDir.I.Play("groan", 0.45f * (1 - d / 9f), Random.Range(0.8f, 1.1f));
            }
        }
    }

    /// <summary>Guards patrol with a sight cone. Being seen outside a shadow means capture.</summary>
    public class Guard : MonoBehaviour, IResettable
    {
        const float Range = 7f;
        float min, max, dir = 1, waitT, alertT = -1, animT;
        Vector3 home;
        SpriteRenderer sr;
        MeshRenderer coneR;
        Mesh cone;
        Color tint;

        public void Init(Ent e, Era era)
        {
            min = e.Min; max = e.Max; home = transform.position;
            tint = era == Era.Future ? new Color(0.75f, 0.7f, 0.65f) : new Color(0.75f, 0.85f, 1f);
            sr = Art.MakeRenderer(gameObject, Art.Char("Soldier", "idle"), 40, tint);
            var cgo = new GameObject("Sight");
            cgo.transform.SetParent(transform, false);
            cone = new Mesh();
            cgo.AddComponent<MeshFilter>().sharedMesh = cone;
            coneR = cgo.AddComponent<MeshRenderer>();
            if (Art.SpriteMat != null) coneR.sharedMaterial = Art.SpriteMat;
            coneR.sortingOrder = 30;
            BuildCone(new Color(1f, 0.92f, 0.45f, 0.22f));
        }

        void BuildCone(Color c)
        {
            float s = dir;
            cone.Clear();
            cone.vertices = new[] { new Vector3(0.2f * s, 1.05f), new Vector3(Range * s, 2.6f), new Vector3(Range * s, -0.3f) };
            cone.colors = new[] { c, new Color(c.r, c.g, c.b, 0f), new Color(c.r, c.g, c.b, 0f) };
            cone.triangles = s > 0 ? new[] { 0, 1, 2 } : new[] { 0, 2, 1 };
            cone.RecalculateBounds();
        }

        public void ResetState()
        {
            transform.position = home; alertT = -1; waitT = 0; dir = 1;
            BuildCone(new Color(1f, 0.92f, 0.45f, 0.22f));
        }

        void Update()
        {
            if (Act.Halted) return;
            var L = LevelRuntime.I; var p = Player.I;
            float dt = Time.deltaTime;
            Vector3 pos = transform.position;
            animT += dt;

            if (alertT >= 0)
            {
                alertT += dt;
                sr.sprite = Art.Char("Soldier", "action1");
                if (alertT > 0.6f) { alertT = -1; L.Kill(); }
                return;
            }

            if (waitT > 0)
            {
                waitT -= dt;
                sr.sprite = Art.Char("Soldier", "idle");
                if (waitT <= 0) { dir = -dir; BuildCone(new Color(1f, 0.92f, 0.45f, 0.22f)); }
            }
            else
            {
                pos.x += dir * 1.7f * dt;
                if ((dir > 0 && pos.x >= max) || (dir < 0 && pos.x <= min))
                {
                    pos.x = Mathf.Clamp(pos.x, min, max);
                    waitT = 1.4f;
                }
                transform.position = pos;
                sr.sprite = Art.Char("Soldier", Mathf.Repeat(animT, 0.5f) < 0.25f ? "walk1" : "walk2");
            }
            sr.flipX = dir < 0;

            Vector2 eye = new Vector2(pos.x, pos.y + 1.05f);
            Vector2 pc = p.Center;
            float dx = pc.x - eye.x, dy = pc.y - eye.y;
            float spread = 0.5f + Mathf.Abs(dx) * 0.22f;
            if (!p.Dead && dx * dir > 0 && Mathf.Abs(dx) < Range && Mathf.Abs(dy) < spread && !L.InShadow(pc) && L.LineClear(eye, pc))
            {
                alertT = 0;
                AudioDir.I.Play("alert", 0.9f);
                BuildCone(new Color(1f, 0.25f, 0.2f, 0.35f));
                L.Caught();
            }
        }
    }

    public class Spikes : MonoBehaviour
    {
        Rect r;
        public void Init(Ent e, Era era)
        {
            r = new Rect(e.X + 0.1f, e.Y, e.W - 0.2f, 0.45f);
            var sr = Art.MakeRenderer(gameObject, Art.Spikes(era), 6);
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(e.W, 0.5f);
            transform.position = new Vector3(e.X + e.W / 2f, e.Y);
        }

        void Update()
        {
            if (Act.Halted) return;
            if (!Player.I.Dead && Player.I.Bounds.Overlaps(r)) LevelRuntime.I.Kill();
        }
    }

    public class Crumble : MonoBehaviour, IResettable
    {
        BoxCollider2D box;
        SpriteRenderer sr;
        Vector3 home;
        int state; // 0 solid, 1 shaking, 2 falling
        float t;

        public void Init(Sprite s)
        {
            home = transform.position;
            box = gameObject.AddComponent<BoxCollider2D>();
            box.size = new Vector2(1f, 1f);
            sr = Art.MakeRenderer(gameObject, s, 4, new Color(1f, 0.85f, 0.75f));
        }

        public void ResetState()
        {
            state = 0; t = 0; transform.position = home; box.enabled = true;
            sr.color = new Color(1f, 0.85f, 0.75f);
        }

        void Update()
        {
            if (Act.Halted) return;
            var p = Player.I;
            float dt = Time.deltaTime;
            if (state == 0)
            {
                bool on = p.Grounded && Mathf.Abs(p.Pos.x - home.x) < 0.78f && Mathf.Abs(p.Pos.y - (home.y + 0.5f)) < 0.15f;
                if (on) { state = 1; t = 0; AudioDir.I.Play("crack", 0.35f, 1.4f); }
            }
            else if (state == 1)
            {
                t += dt;
                transform.position = home + (Vector3)(Random.insideUnitCircle * 0.05f);
                if (t > 0.45f) { state = 2; t = 0; box.enabled = false; }
            }
            else
            {
                t += dt;
                transform.position = home + Vector3.down * (t * t * 12f);
                sr.color = new Color(1f, 0.85f, 0.75f, Mathf.Clamp01(1 - t));
                if (t > 3.5f && !p.Bounds.Overlaps(new Rect(home.x - 0.5f, home.y - 0.5f, 1, 1))) ResetState();
            }
        }
    }

    public class Gas : MonoBehaviour
    {
        Rect r;
        float phase, inside;
        SpriteRenderer sr;
        bool hissed;
        const float Period = 4.6f, OnTime = 2.4f;

        public void Init(Ent e)
        {
            r = new Rect(e.X, e.Y, e.W, e.H);
            phase = e.Phase;
            transform.position = new Vector3(e.X + e.W / 2f, e.Y + e.H / 2f);
            sr = Art.MakeRenderer(gameObject, Art.Pixel, 55, new Color(0.5f, 0.9f, 0.3f, 0.1f));
            transform.localScale = new Vector3(e.W, e.H, 1);
        }

        void Update()
        {
            if (Act.Halted) return;
            float c = Mathf.Repeat(Time.time + phase, Period);
            bool on = c < OnTime;
            bool warn = c > Period - 0.7f;
            float a = on ? 0.5f + 0.1f * Mathf.Sin(Time.time * 9) : warn ? 0.25f : 0.07f;
            sr.color = new Color(0.5f, 0.9f, 0.3f, a);
            var p = Player.I;
            if (warn && !hissed && Mathf.Abs(p.Pos.x - r.center.x) < 9) { hissed = true; AudioDir.I.Play("hiss", 0.5f); }
            if (!warn) hissed = false;
            if (on && r.Contains(p.Center)) inside += Time.deltaTime; else inside = 0;
            if (inside > 0.25f) { inside = 0; LevelRuntime.I.Kill(); }
        }
    }

    public class CheckpointMarker : MonoBehaviour
    {
        bool lit;
        SpriteRenderer sr, glow;
        public void Init()
        {
            sr = Art.MakeRenderer(gameObject, Art.Prop("lanternOff"), 5);
            var g = new GameObject("Glow");
            g.transform.SetParent(transform, false);
            g.transform.localPosition = new Vector3(0, 1.6f);
            g.transform.localScale = Vector3.one * 0.6f;
            glow = Art.MakeRenderer(g, Art.Prop("glow"), 4, new Color(1f, 0.8f, 0.4f, 0f));
        }

        void Update()
        {
            if (Act.Halted || lit) return;
            var p = Player.I;
            if (Mathf.Abs(p.Pos.x - transform.position.x) < 0.8f && Mathf.Abs(p.Pos.y - transform.position.y) < 2f)
            {
                lit = true;
                sr.sprite = Art.Prop("lantern");
                glow.color = new Color(1f, 0.8f, 0.4f, 0.6f);
                LevelRuntime.I.SetCheckpoint(transform.position);
                AudioDir.I.Play("checkpoint", 0.6f);
            }
        }
    }

    public class TextZone : MonoBehaviour
    {
        public Ent E;
        void Update()
        {
            if (Act.Halted) return;
            var p = Player.I;
            if (Mathf.Abs(p.Pos.x - E.X) < E.Radius && Mathf.Abs(p.Pos.y - E.Y) < 3f)
            {
                string t = LevelRuntime.I.InA ? E.TextA : E.TextB;
                if (!string.IsNullOrEmpty(t)) UI.I.Caption(this, t, Mathf.Abs(p.Pos.x - E.X));
            }
        }
    }

    public class Npc : MonoBehaviour
    {
        public Ent E;
        public string Pose;
        public bool Talking;
        SpriteRenderer sr;
        float t;

        public void Init(Ent e)
        {
            E = e;
            sr = Art.MakeRenderer(gameObject, Art.Char(e.Set, "idle"), 45, e.Tint);
            sr.flipX = e.FaceLeft;
        }

        public void Face(bool left) { E.FaceLeft = left; sr.flipX = left; }

        void Update()
        {
            t += Time.deltaTime;
            string pose = Pose ?? (Talking ? (Mathf.Repeat(t, 0.4f) < 0.2f ? "talk" : "idle") : (Mathf.Repeat(t, 2.4f) < 2.0f ? "idle" : "stand"));
            sr.sprite = Art.Char(E.Set, pose);
            sr.flipX = E.FaceLeft;
        }
    }

    public class Lamp : MonoBehaviour
    {
        bool used;
        SpriteRenderer glow;
        public void Init()
        {
            Art.MakeRenderer(gameObject, Art.Prop("lantern"), 5);
            var g = new GameObject("Glow");
            g.transform.SetParent(transform, false);
            g.transform.localPosition = new Vector3(0, 1.6f);
            glow = Art.MakeRenderer(g, Art.Prop("glow"), 4, new Color(0.5f, 0.9f, 1f, 0.7f));
        }

        void Update()
        {
            if (Act.Halted || used) return;
            glow.transform.localScale = Vector3.one * (0.8f + 0.1f * Mathf.Sin(Time.time * 4));
            var p = Player.I;
            if (Mathf.Abs(p.Pos.x - transform.position.x) < 0.9f && Mathf.Abs(p.Pos.y - transform.position.y) < 2f)
            {
                var L = LevelRuntime.I;
                if (L.Charges >= 0 && L.Charges < 3)
                {
                    used = true;
                    L.Charges++;
                    glow.color = new Color(0.5f, 0.9f, 1f, 0.1f);
                    AudioDir.I.Play("refill", 0.7f);
                    UI.I.Toast("Otto's lamp wakes the Tether. +1 charge");
                }
            }
        }
    }

    public class Beam : MonoBehaviour, IResettable
    {
        Vector3 home;
        int state; float t, vy;
        SpriteRenderer sr;

        public void Init()
        {
            home = transform.position;
            sr = Art.MakeRenderer(gameObject, Art.Prop("beam"), 7);
        }

        public void ResetState() { state = 0; t = 0; vy = 0; transform.position = home; sr.color = Color.white; gameObject.SetActive(true); }

        void Update()
        {
            if (Act.Halted) return;
            var p = Player.I; var L = LevelRuntime.I;
            float dt = Time.deltaTime;
            Vector3 pos = transform.position;
            switch (state)
            {
                case 0:
                    if (Mathf.Abs(p.Pos.x - home.x) < 2.2f && p.Pos.y < home.y) { state = 1; t = 0; AudioDir.I.Play("crack", 0.6f); }
                    break;
                case 1:
                    t += dt;
                    transform.position = home + (Vector3)(Random.insideUnitCircle * 0.06f);
                    if (t > 0.4f) { state = 2; transform.position = home; }
                    break;
                case 2:
                    vy -= 30f * dt;
                    pos.y += vy * dt;
                    transform.position = pos;
                    if (!p.Dead && p.Bounds.Overlaps(new Rect(pos.x - 1.4f, pos.y - 0.25f, 2.8f, 0.5f))) L.Kill();
                    if (L.SolidAt(pos.x, pos.y - 0.3f) || pos.y < -2) { state = 3; t = 0; AudioDir.I.Play("land", 0.8f, 0.6f); Cam.I.Shake(0.2f); }
                    break;
                case 3:
                    t += dt;
                    sr.color = new Color(1, 1, 1, Mathf.Clamp01(1 - t));
                    break;
            }
        }
    }

    public class TetherPickup : MonoBehaviour
    {
        bool taken;
        Vector3 home;
        SpriteRenderer glow;
        public void Init()
        {
            home = transform.position;
            Art.MakeRenderer(gameObject, Art.Prop("tether"), 20);
            var g = new GameObject("Glow");
            g.transform.SetParent(transform, false);
            g.transform.localScale = Vector3.one * 0.9f;
            glow = Art.MakeRenderer(g, Art.Prop("glow"), 19, new Color(1f, 0.85f, 0.4f, 0.7f));
        }

        void Update()
        {
            if (taken) return;
            transform.position = home + Vector3.up * (0.15f * Mathf.Sin(Time.time * 2.5f));
            glow.transform.localScale = Vector3.one * (0.85f + 0.12f * Mathf.Sin(Time.time * 5));
            if (Act.Halted) return;
            if (Player.I.Bounds.Overlaps(new Rect(home.x - 0.5f, home.y - 0.5f, 1f, 1.2f)))
            {
                taken = true;
                gameObject.SetActive(false);
                LevelRuntime.I.RunCue("tether", null);
            }
        }
    }

    /// <summary>The collapsing cave in Level 5. It follows the player and kills on contact.</summary>
    public class CaveWall : MonoBehaviour
    {
        public float X;
        public bool Active;
        SpriteRenderer sr;

        public void Init(int h)
        {
            sr = Art.MakeRenderer(gameObject, Art.Pixel, 70, new Color(0.08f, 0.05f, 0.04f, 1f));
            transform.localScale = new Vector3(40, h + 10, 1);
            for (int i = 0; i < 10; i++)
            {
                var r = new GameObject("Rock");
                r.transform.SetParent(transform, false);
                r.transform.localPosition = new Vector3(0.5f, (i / 10f) - 0.45f, 0);
                r.transform.localScale = new Vector3(1.6f / 40f, 1.6f / (h + 10), 1);
                Art.MakeRenderer(r, Art.Prop("rock"), 71);
            }
        }

        public void Place(float x) { X = x; Sync(); }

        void Sync()
        {
            transform.position = new Vector3(X - 20f, LevelRuntime.I.P.H / 2f, 0);
            foreach (Transform c in transform)
                c.localPosition = new Vector3(0.5f + Mathf.Sin(Time.time * 7 + c.GetSiblingIndex()) * 0.004f, c.localPosition.y, 0);
        }

        void Update()
        {
            if (!Active || Act.Halted) return;
            var p = Player.I;
            float gap = p.Pos.x - X;
            float speed = LevelRuntime.I.P.ChaseSpeed + (gap > 14 ? 2.0f : 0);
            X += speed * Time.deltaTime;
            Sync();
            Cam.I.Shake(0.06f);
            if (!p.Dead && p.Pos.x - 0.3f < X) LevelRuntime.I.Kill();
        }
    }
}
