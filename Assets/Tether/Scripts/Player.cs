using UnityEngine;

namespace Tether
{
    /// <summary>Wren, the playable scavenger. Run, jump, climb ladders, duck and slide.</summary>
    public class Player : MonoBehaviour
    {
        public static Player I;

        public Rigidbody2D rb;
        public CapsuleCollider2D col;
        public SpriteRenderer sr;

        public bool Frozen, Dead, Grounded, Climbing, Ducking, FacingLeft;
        public string PoseOverride;
        public float ScriptMoveX; // used by cutscenes to walk Wren

        const float Speed = 6f, JumpV = 13.5f, Grav = 30f, ClimbSpeed = 4f;
        float coyote, jumpBuf, animT, stepT, slideT, lastVy;
        bool jumpCut, ignoringTops;
        int mask;

        static readonly Vector2 StandSize = new Vector2(0.55f, 1.15f), StandOff = new Vector2(0, 0.6f);
        static readonly Vector2 DuckSize = new Vector2(0.6f, 0.62f), DuckOff = new Vector2(0, 0.33f);

        public Vector2 Pos { get { return rb.position; } }
        public Vector2 Vel
        {
            get
            {
#if UNITY_6000_0_OR_NEWER
                return rb.linearVelocity;
#else
                return rb.velocity;
#endif
            }
            set
            {
#if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = value;
#else
                rb.velocity = value;
#endif
            }
        }

        public Rect Bounds
        {
            get
            {
                Vector2 s = col.size;
                Vector2 c = Pos + col.offset;
                return new Rect(c.x - s.x / 2, c.y - s.y / 2, s.x, s.y);
            }
        }

        public Vector2 Center { get { return Pos + col.offset; } }

        public static Player Create(Vector2 at)
        {
            var go = new GameObject("Wren");
            go.layer = 2; // Ignore Raycast: keeps our own ground checks from hitting ourselves
            go.transform.position = at;
            var p = go.AddComponent<Player>();
            return p;
        }

        void Awake()
        {
            I = this;
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.gravityScale = Grav / Mathf.Max(0.1f, -Physics2D.gravity.y);
            col = gameObject.AddComponent<CapsuleCollider2D>();
            col.size = StandSize; col.offset = StandOff;
            col.sharedMaterial = new PhysicsMaterial2D("NoFriction") { friction = 0f, bounciness = 0f };
            sr = Art.MakeRenderer(gameObject, Art.Char("Player", "idle"), 50);
            mask = ~(1 << 2);
        }

        public void Teleport(Vector2 p)
        {
            rb.position = p;
            transform.position = p;
            Vel = Vector2.zero;
            Climbing = false;
            SetDuck(false);
            SetIgnoreTops(false);
            slideT = 0;
        }

        bool Paused { get { return Game.I != null && Game.I.Paused; } }
        bool CanAct { get { return !Frozen && !Dead && !Paused && LevelRuntime.I != null && !LevelRuntime.I.Busy; } }

        void Update()
        {
            if (Paused) return;
            if (CanAct)
            {
                if (GInput.JumpDown) jumpBuf = 0.13f;
                if (GInput.ShiftDown) LevelRuntime.I.TryShift(true);
            }
            Animate();
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            bool act = CanAct;
            float mx = act ? GInput.MoveX : 0, my = act ? GInput.MoveY : 0;
            Vector2 v = Vel;
            var L = LevelRuntime.I;

            bool wasGrounded = Grounded;
            Grounded = v.y <= 0.1f && Physics2D.OverlapBox(Pos + new Vector2(0, -0.04f), new Vector2(0.46f, 0.08f), 0, mask) != null;
            coyote = Grounded ? 0.1f : coyote - dt;
            jumpBuf -= dt;

            float feet = Pos.y;
            float cx = Pos.x;
            bool ladderHere = L != null && (L.LadderAt(cx, feet + 0.5f) || L.LadderAt(cx, feet + 0.05f));
            bool ladderBelow = L != null && L.LadderAt(cx, feet - 0.3f);

            if (!Climbing && L != null)
            {
                if ((ladderHere && my > 0.5f) || (ladderBelow && Grounded && my < -0.5f && !ladderHere))
                {
                    Climbing = true;
                    SetDuck(false);
                    SetIgnoreTops(true);
                    rb.position = new Vector2(Mathf.Floor(cx) + 0.5f, Pos.y + (my < 0 ? -0.15f : 0.02f));
                }
            }

            if (Climbing)
            {
                rb.gravityScale = 0;
                v = new Vector2(0, my * ClimbSpeed);
                bool ladderAtFeet = L.LadderAt(cx, feet + 0.05f);
                if (my > 0 && !ladderAtFeet)
                {
                    // reached the top: pop up onto the ledge
                    Climbing = false;
                    SetIgnoreTops(false);
                    v = new Vector2(mx * Speed * 0.5f, 5f);
                }
                else if (my < 0 && !L.LadderAt(cx, feet - 0.05f) && Grounded)
                {
                    Climbing = false; SetIgnoreTops(false);
                }
                else if (jumpBuf > 0)
                {
                    Climbing = false; SetIgnoreTops(false);
                    v = new Vector2(mx * Speed, JumpV * 0.75f);
                    jumpBuf = 0;
                    AudioDir.I.Play("jump", 0.6f);
                }
                else if (!ladderHere && !ladderAtFeet)
                {
                    Climbing = false; SetIgnoreTops(false);
                }
            }

            if (!Climbing)
            {
                rb.gravityScale = Grav / Mathf.Max(0.1f, -Physics2D.gravity.y);
                bool wantDuck = Grounded && my < -0.5f && !ladderBelow;
                if (wantDuck && !Ducking)
                {
                    SetDuck(true);
                    if (Mathf.Abs(v.x) > 4f) { slideT = 0.5f; AudioDir.I.Play("land", 0.4f, 1.4f); }
                }
                else if (!wantDuck && Ducking && slideT <= 0 && HeadroomClear()) SetDuck(false);

                float target = mx * Speed;
                if (Ducking) target = slideT > 0 ? (FacingLeft ? -1 : 1) * Speed * 1.15f : mx * 2.4f;
                slideT -= dt;
                if (!act && ScriptMoveX != 0) { target = ScriptMoveX; FacingLeft = ScriptMoveX < 0; }
                float accel = Grounded ? 70f : 45f;
                if (slideT > 0) v.x = target; else v.x = Mathf.MoveTowards(v.x, target, accel * dt);

                if (jumpBuf > 0 && coyote > 0 && (!Ducking || HeadroomClear()))
                {
                    SetDuck(false);
                    v.y = JumpV;
                    jumpBuf = 0; coyote = 0; jumpCut = false; slideT = 0;
                    AudioDir.I.Play("jump", 0.55f);
                }
                if (!GInput.JumpHeld && v.y > 0 && !jumpCut && !Grounded) { v.y *= 0.5f; jumpCut = true; }
                if (v.y < -22f) v.y = -22f;
            }

            if (act && Mathf.Abs(mx) > 0.1f && slideT <= 0) FacingLeft = mx < 0;

            if (Grounded && !wasGrounded && lastVy < -7f) AudioDir.I.Play("land", 0.6f);
            if (Grounded && Mathf.Abs(v.x) > 1f && !Ducking)
            {
                stepT -= dt;
                if (stepT <= 0) { stepT = 0.28f; AudioDir.I.Play("step", 0.35f, Random.Range(0.85f, 1.15f)); }
            }
            lastVy = v.y;
            Vel = v;
        }

        bool HeadroomClear()
        {
            return Physics2D.OverlapBox(Pos + new Vector2(0, 0.95f), new Vector2(0.5f, 0.55f), 0, mask) == null;
        }

        void SetDuck(bool d)
        {
            Ducking = d;
            col.size = d ? DuckSize : StandSize;
            col.offset = d ? DuckOff : StandOff;
        }

        void SetIgnoreTops(bool ignore)
        {
            if (ignoringTops == ignore || LevelRuntime.I == null) return;
            ignoringTops = ignore;
            foreach (var c in LevelRuntime.I.LadderTops) if (c != null) Physics2D.IgnoreCollision(col, c, ignore);
        }

        void Animate()
        {
            animT += Time.deltaTime;
            Vector2 v = Vel;
            string pose;
            if (PoseOverride != null) pose = PoseOverride;
            else if (Dead) pose = "hurt";
            else if (Climbing) pose = Mathf.Abs(v.y) > 0.1f ? (Mathf.Repeat(animT, 0.3f) < 0.15f ? "climb1" : "climb2") : "climb1";
            else if (Ducking) pose = slideT > 0 ? "slide" : "duck";
            else if (!Grounded) pose = v.y > 0 ? "jump" : "fall";
            else if (Mathf.Abs(v.x) > 0.5f) pose = Mathf.Repeat(animT, 0.26f) < 0.13f ? "walk1" : "walk2";
            else pose = "idle";
            sr.sprite = Art.Char("Player", pose);
            sr.flipX = FacingLeft;
        }
    }
}
