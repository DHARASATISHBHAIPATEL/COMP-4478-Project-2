using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tether
{
    /// <summary>Follow camera with look-ahead, bounds, shake, zoom pulse and scripted pans.</summary>
    public class Cam : MonoBehaviour
    {
        public static Cam I;
        public Camera cam;
        public const float BaseSize = 6.2f;

        Transform target;
        Rect bounds;
        bool hasBounds, overriding;
        Vector2 pos, vel, ovPos;
        float size = BaseSize, ovSize, shake, zoom, lookAhead;

        void Awake()
        {
            I = this;
            cam = GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = BaseSize;
            cam.clearFlags = CameraClearFlags.SolidColor;
        }

        public void SetBounds(Rect r) { bounds = r; hasBounds = true; }
        public void ClearBounds() { hasBounds = false; }

        public void Follow(Transform t, bool snap)
        {
            target = t;
            overriding = false;
            if (snap && t != null) { pos = Desired(); vel = Vector2.zero; size = BaseSize; }
        }

        Vector2 Desired()
        {
            return (Vector2)target.position + new Vector2(lookAhead, 1.6f);
        }

        public void Hold(Vector2 at, float sz)
        {
            overriding = true;
            pos = ovPos = at;
            ovSize = size = sz;
        }

        public IEnumerator PanTo(Vector2 to, float sz, float dur)
        {
            overriding = true;
            Vector2 from = pos;
            float s0 = size;
            float t = 0;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0, 1, t / dur);
                ovPos = Vector2.Lerp(from, to, k);
                ovSize = Mathf.Lerp(s0, sz, k);
                pos = ovPos; size = ovSize;
                yield return null;
            }
            ovPos = pos = to;
            ovSize = size = sz;
        }

        public void Release() { overriding = false; }
        public void Shake(float a) { shake = Mathf.Max(shake, a); }
        public void ZoomPulse() { zoom = 1f; }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (!overriding && target != null)
            {
                var p = target.GetComponent<Player>();
                float want = 0;
                if (p != null && Mathf.Abs(p.Vel.x) > 1f) want = p.FacingLeft ? -2f : 2f;
                lookAhead = Mathf.MoveTowards(lookAhead, want, dt * 3f);
                pos = Vector2.SmoothDamp(pos, Desired(), ref vel, 0.16f, 60f, dt);
                size = Mathf.MoveTowards(size, BaseSize, dt * 3f);
            }
            zoom = Mathf.MoveTowards(zoom, 0, dt * 2.5f);
            cam.orthographicSize = size * (1f - 0.06f * Mathf.Sin(zoom * Mathf.PI));

            Vector2 p2 = pos;
            if (hasBounds && !overriding)
            {
                float hh = cam.orthographicSize, hw = hh * cam.aspect;
                p2.x = bounds.width < hw * 2 ? bounds.center.x : Mathf.Clamp(p2.x, bounds.xMin + hw, bounds.xMax - hw);
                float minY = bounds.yMin - 1.5f + hh, maxY = bounds.yMax + 2f - hh;
                p2.y = maxY < minY ? (minY + maxY) / 2 : Mathf.Clamp(p2.y, minY, maxY);
            }
            if (shake > 0)
            {
                p2 += Random.insideUnitCircle * shake * 0.5f;
                shake = Mathf.MoveTowards(shake, 0, dt * 1.5f);
            }
            transform.position = new Vector3(p2.x, p2.y, -10f);
        }
    }

    /// <summary>Sky gradient, two parallax skyline layers and ambient particles (ash, dust, petals).</summary>
    public class Backdrop : MonoBehaviour
    {
        public static Backdrop I;
        SpriteRenderer sky;
        readonly SpriteRenderer[] layers = new SpriteRenderer[2];
        readonly float[] factors = { 0.85f, 0.6f };
        readonly List<Transform> motes = new List<Transform>();
        readonly List<Vector2> moteVel = new List<Vector2>();
        Camera cam;
        Era era;
        bool under;

        public void Init(Camera c)
        {
            I = this;
            cam = c;
            sky = Make("Sky", -100);
            for (int i = 0; i < 2; i++)
            {
                layers[i] = Make("Skyline" + i, -90 + i * 5);
                layers[i].drawMode = SpriteDrawMode.Tiled;
            }
            for (int i = 0; i < 46; i++)
            {
                var m = Make("Mote", 80);
                m.sprite = Art.Pixel;
                m.transform.localScale = Vector3.one * Random.Range(0.05f, 0.12f);
                motes.Add(m.transform);
                moteVel.Add(Vector2.zero);
            }
            Set(Era.Future, false);
        }

        SpriteRenderer Make(string n, int order)
        {
            var go = new GameObject(n);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0, 0, 20);
            return Art.MakeRenderer(go, null, order);
        }

        public void Set(Era e, bool underground)
        {
            era = e; under = underground;
            if (UI.I != null) UI.I.SetEraGrade(e, underground);
            var p = Art.Pal(e, underground);
            cam.backgroundColor = p.SkyBottom;
            sky.sprite = Art.SkyGradient(e, underground);
            for (int i = 0; i < 2; i++)
            {
                layers[i].sprite = Art.Skyline(e, i, underground);
                layers[i].size = new Vector2(80, 6);
            }
            bool none = e == Era.Modern && !underground;
            for (int i = 0; i < motes.Count; i++)
            {
                var sr = motes[i].GetComponent<SpriteRenderer>();
                Color c = p.Particle;
                if (underground) c = new Color(1f, 0.85f, 0.55f, 0.35f);
                sr.color = none && i % 3 != 0 ? Color.clear : c;
                Vector2 v;
                switch (e)
                {
                    case Era.Future: v = new Vector2(Random.Range(-0.6f, 0.2f), Random.Range(-1.2f, -0.5f)); break;
                    case Era.Saved: v = new Vector2(Random.Range(0.3f, 1.0f), Random.Range(-0.9f, -0.4f)); break;
                    default: v = new Vector2(Random.Range(-0.2f, 0.2f), Random.Range(-0.1f, 0.25f)); break;
                }
                moteVel[i] = v;
                if (motes[i].localPosition.z != 15) motes[i].localPosition = new Vector3(Random.Range(-14f, 14f), Random.Range(-8f, 8f), 15);
            }
        }

        void LateUpdate()
        {
            if (cam == null) return;
            float hh = cam.orthographicSize, hw = hh * cam.aspect;
            sky.transform.localScale = new Vector3(hw * 2 + 2, (hh * 2 + 2) / 64f, 1);
            Vector3 cp = cam.transform.position;
            for (int i = 0; i < 2; i++)
            {
                float f = factors[i];
                float x = -Mathf.Repeat(cp.x * (1 - f), 16f);
                float y = -hh - 0.5f + (i == 0 ? 2.2f : 0.6f) - Mathf.Clamp((cp.y - 8f) * (1 - f) * 0.4f, -3f, 3f);
                if (under && i == 0) y = -hh - 0.5f;
                layers[i].transform.localPosition = new Vector3(x, y, 19 - i);
                if (under) layers[i].size = new Vector2(80, hh * 2 + 2); else layers[i].size = new Vector2(80, 6);
            }
            float dt = Time.deltaTime;
            for (int i = 0; i < motes.Count; i++)
            {
                var t = motes[i];
                Vector3 lp = t.localPosition;
                lp.x += (moteVel[i].x + Mathf.Sin(Time.time * 0.7f + i) * 0.15f) * dt;
                lp.y += moteVel[i].y * dt;
                if (lp.x < -hw - 1) lp.x += hw * 2 + 2;
                if (lp.x > hw + 1) lp.x -= hw * 2 + 2;
                if (lp.y < -hh - 1) lp.y += hh * 2 + 2;
                if (lp.y > hh + 1) lp.y -= hh * 2 + 2;
                t.localPosition = lp;
            }
        }
    }
}
