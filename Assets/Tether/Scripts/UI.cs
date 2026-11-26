using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tether
{
    public class MenuItem
    {
        public string Label;
        public Func<string> Value;      // null = plain button
        public Action<int> Change;      // left/right on an option
        public Action Confirm;          // press on a button
        public MenuItem(string label, Action confirm) { Label = label; Confirm = confirm; }
        public MenuItem(string label, Func<string> value, Action<int> change) { Label = label; Value = value; Change = change; Confirm = () => change(1); }
    }

    public class Menu
    {
        public string Title, Footer;
        public List<MenuItem> Items = new List<MenuItem>();
        public int Sel;
        public Action OnBack;
        public bool Opaque;
    }

    /// <summary>
    /// All on-screen presentation drawn with IMGUI so the project needs no UI
    /// prefabs: HUD, story captions, dialogue boxes, chapter cards, letterbox,
    /// fades, era colour grade, and the controller-friendly menus.
    /// </summary>
    public class UI : MonoBehaviour
    {
        public static UI I;
        const float VH = 1080f;

        public float LetterboxTarget;
        float letterbox, fade, flash, captionA, toastT, cardA;
        string caption, pendingCaption, toast, cardBig, cardSmall;
        float pendingDist = 999;
        string dlgName, dlgText;
        int dlgShown;
        bool dlgActive;
        Color tint = Color.clear;
        float vignette = 0.5f;

        // HUD
        bool hud;
        string hudEra;
        int hudCharges = -1;
        bool hudShift;

        // menus
        readonly Stack<Menu> menus = new Stack<Menu>();
        public bool MenuOpen { get { return menus.Count > 0; } }
        public bool TitleMode;
        Vector2 lastMouse;

        // credits
        string[] credits;
        float creditsY;

        GUIStyle sBig, sMid, sText, sSmall, sItem, sName;

        void Awake() { I = this; }

        // ---------------- public API ----------------

        public void SetEraGrade(Era e, bool underground)
        {
            switch (e)
            {
                case Era.Future: tint = new Color(0.35f, 0.12f, 0.05f, 0.12f); vignette = 0.75f; break;
                case Era.Modern: tint = new Color(0.0f, 0.15f, 0.3f, 0.05f); vignette = 0.3f; break;
                case Era.Past: tint = new Color(0.55f, 0.35f, 0.1f, 0.12f); vignette = 0.6f; break;
                default: tint = new Color(1f, 0.9f, 0.6f, 0.06f); vignette = 0.2f; break;
            }
            if (underground) vignette = 0.9f;
        }

        public void Caption(object owner, string text, float dist)
        {
            if (dist < pendingDist) { pendingCaption = text; pendingDist = dist; }
        }

        public void Toast(string t) { toast = t; toastT = 2.6f; }
        public void Flash(float a) { flash = Mathf.Max(flash, a); }
        public void SetFade(float f) { fade = f; }

        public void Hud(LevelRuntime L)
        {
            hud = !L.Busy && !L.Finished;
            hudEra = Art.EraLabel(L.Current);
            hudCharges = L.P.ShiftEnabled ? L.Charges : -2;
            hudShift = L.P.ShiftEnabled;
        }

        public void HideHud() { hud = false; }

        public void ResetOverlays()
        {
            dlgActive = false; cardA = 0; credits = null; LetterboxTarget = 0; letterbox = 0;
            captionA = 0; caption = null; toastT = 0; hud = false; flash = 0;
        }

        public IEnumerator FadeTo(float target, float dur)
        {
            float start = fade, t = 0;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                if (Game.I != null && Game.I.Paused) { yield return null; continue; }
                fade = Mathf.Lerp(start, target, t / dur);
                yield return null;
            }
            fade = target;
        }

        static bool Advance
        {
            get { return !(Game.I != null && Game.I.Paused) && (GInput.ConfirmDown || GInput.JumpDown || GInput.InteractDown); }
        }

        public IEnumerator Say(string name, string text)
        {
            dlgName = name;
            dlgText = GInput.Fill(text);
            dlgShown = 0;
            dlgActive = true;
            float t = 0;
            yield return null;
            while (dlgShown < dlgText.Length)
            {
                if (!(Game.I != null && Game.I.Paused))
                {
                    t += Time.deltaTime * 48f;
                    int n = Mathf.Min((int)t, dlgText.Length);
                    if (n > dlgShown) { if (n / 3 != dlgShown / 3) AudioDir.I.Play("tick", 0.5f, UnityEngine.Random.Range(0.9f, 1.1f)); dlgShown = n; }
                    if (Advance && t > 3) dlgShown = dlgText.Length;
                }
                yield return null;
            }
            yield return null;
            while (!Advance) yield return null;
            AudioDir.I.Play("move", 0.6f);
            dlgActive = false;
            yield return null;
        }

        /// <summary>Big centered title card, e.g. a chapter name.</summary>
        public IEnumerator Card(string big, string small, float hold)
        {
            cardBig = big; cardSmall = small;
            for (float t = 0; t < 0.8f; t += Time.deltaTime) { cardA = t / 0.8f; yield return null; }
            cardA = 1;
            yield return new WaitForSeconds(hold);
            for (float t = 0; t < 0.8f; t += Time.deltaTime) { cardA = 1 - t / 0.8f; yield return null; }
            cardA = 0;
        }

        public IEnumerator Credits(string[] lines, float speed = 55f)
        {
            credits = lines;
            creditsY = VH + 40;
            float end = -lines.Length * 64f - 40;
            while (creditsY > end)
            {
                float k = (GInput.ConfirmDown || GInput.JumpDown) ? 6f : 1f;
                creditsY -= Time.deltaTime * speed * k;
                yield return null;
            }
            credits = null;
        }

        public void OpenMenu(Menu m) { menus.Push(m); AudioDir.I.Play("confirm", 0.5f); GInput.Consume(); }
        public void CloseMenu() { if (menus.Count > 0) menus.Pop(); AudioDir.I.Play("back", 0.5f); GInput.Consume(); }
        public void CloseAllMenus() { menus.Clear(); }

        // ---------------- update ----------------

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            letterbox = Mathf.MoveTowards(letterbox, LetterboxTarget, dt * 2.2f);
            flash = Mathf.MoveTowards(flash, 0, dt * 1.6f);
            if (!(Game.I != null && Game.I.Paused)) toastT -= dt;

            if (menus.Count > 0)
            {
                var m = menus.Peek();
                if (GInput.NavUp) { m.Sel = (m.Sel + m.Items.Count - 1) % m.Items.Count; AudioDir.I.Play("move", 0.5f); }
                if (GInput.NavDown) { m.Sel = (m.Sel + 1) % m.Items.Count; AudioDir.I.Play("move", 0.5f); }
                var it = m.Items[m.Sel];
                if (it.Change != null && (GInput.NavLeft || GInput.NavRight)) { it.Change(GInput.NavLeft ? -1 : 1); AudioDir.I.Play("move", 0.5f); }
                else if (GInput.ConfirmDown && it.Confirm != null) { AudioDir.I.Play("confirm", 0.5f); GInput.Consume(); it.Confirm(); }
                else if (GInput.BackDown && m.OnBack != null) { GInput.Consume(); m.OnBack(); }
            }
        }

        void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;
            if (pendingCaption != null)
            {
                if (caption != pendingCaption && captionA > 0.05f) captionA = Mathf.MoveTowards(captionA, 0, dt * 6);
                else { caption = pendingCaption; captionA = Mathf.MoveTowards(captionA, 1, dt * 3); }
            }
            else captionA = Mathf.MoveTowards(captionA, 0, dt * 2.5f);
            pendingCaption = null;
            pendingDist = 999;
        }

        // ---------------- drawing ----------------

        void Styles()
        {
            if (sBig != null) return;
            sBig = new GUIStyle(GUI.skin.label) { fontSize = 120, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, wordWrap = true };
            sMid = new GUIStyle(GUI.skin.label) { fontSize = 46, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            sText = new GUIStyle(GUI.skin.label) { fontSize = 36, alignment = TextAnchor.UpperLeft, wordWrap = true, richText = true };
            sSmall = new GUIStyle(GUI.skin.label) { fontSize = 28, alignment = TextAnchor.MiddleLeft, wordWrap = false };
            sItem = new GUIStyle(GUI.skin.label) { fontSize = 40, alignment = TextAnchor.MiddleLeft };
            sName = new GUIStyle(GUI.skin.label) { fontSize = 34, alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold };
        }

        static void Box(Rect r, Color c)
        {
            var old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Art.White);
            GUI.color = old;
        }

        static void Shadowed(Rect r, string text, GUIStyle s, Color c)
        {
            var old = s.normal.textColor;
            s.normal.textColor = new Color(0, 0, 0, c.a * 0.7f);
            GUI.Label(new Rect(r.x + 3, r.y + 3, r.width, r.height), text, s);
            s.normal.textColor = c;
            GUI.Label(r, text, s);
            s.normal.textColor = old;
        }

        void OnGUI()
        {
            Styles();
            float scale = Screen.height / VH;
            float vw = Screen.width / scale;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1));
            GUI.depth = 0;

            // era colour grade + vignette
            if (tint.a > 0) Box(new Rect(0, 0, vw, VH), tint);
            var oc = GUI.color;
            GUI.color = new Color(1, 1, 1, vignette);
            GUI.DrawTexture(new Rect(-40, -40, vw + 80, VH + 80), Art.Vignette());
            GUI.color = oc;

            // letterbox bars
            if (letterbox > 0)
            {
                float h = 130 * Mathf.SmoothStep(0, 1, letterbox);
                Box(new Rect(0, 0, vw, h), Color.black);
                Box(new Rect(0, VH - h, vw, h), Color.black);
            }

            if (hud && !TitleMode && menus.Count == 0) DrawHud(vw);

            // story caption (environmental text)
            if (captionA > 0.01f && !string.IsNullOrEmpty(caption) && !dlgActive)
            {
                float w = Mathf.Min(1300, vw - 120);
                var r = new Rect((vw - w) / 2, VH - 300, w, 170);
                Box(new Rect(r.x - 30, r.y - 18, r.width + 60, r.height + 20), new Color(0, 0, 0, 0.55f * captionA));
                var st = new GUIStyle(sText) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Italic };
                Shadowed(r, GInput.Fill(caption), st, new Color(1, 0.96f, 0.88f, captionA));
            }

            // toast
            if (toastT > 0 && !string.IsNullOrEmpty(toast))
            {
                float a = Mathf.Clamp01(toastT * 2);
                var st = new GUIStyle(sSmall) { alignment = TextAnchor.MiddleCenter, fontSize = 32 };
                Box(new Rect(vw / 2 - 520, 150, 1040, 64), new Color(0, 0, 0, 0.5f * a));
                Shadowed(new Rect(vw / 2 - 520, 150, 1040, 64), GInput.Fill(toast), st, new Color(1, 1, 1, a));
            }

            // dialogue box
            if (dlgActive)
            {
                float w = Mathf.Min(1400, vw - 100);
                var r = new Rect((vw - w) / 2, VH - 330, w, 210);
                Box(r, new Color(0.04f, 0.04f, 0.07f, 0.85f));
                Box(new Rect(r.x, r.y, 8, r.height), new Color(1f, 0.8f, 0.4f, 0.9f));
                Shadowed(new Rect(r.x + 40, r.y + 14, w - 80, 48), dlgName, sName, new Color(1f, 0.82f, 0.45f));
                Shadowed(new Rect(r.x + 40, r.y + 62, w - 80, 140), dlgText.Substring(0, dlgShown), sText, Color.white);
                if (dlgShown >= dlgText.Length && Mathf.Repeat(Time.unscaledTime, 1f) < 0.7f)
                    Shadowed(new Rect(r.xMax - 260, r.yMax - 46, 240, 40), GInput.Key("CONFIRM") + "  >", new GUIStyle(sSmall) { alignment = TextAnchor.MiddleRight }, new Color(1, 1, 1, 0.8f));
            }

            // chapter / title card
            if (cardA > 0.01f)
            {
                Shadowed(new Rect(0, VH / 2 - 150, vw, 180), cardBig, sBig, new Color(1, 1, 1, cardA));
                if (!string.IsNullOrEmpty(cardSmall))
                    Shadowed(new Rect(100, VH / 2 + 40, vw - 200, 140), cardSmall, sMid, new Color(1, 0.92f, 0.8f, cardA));
            }

            // credits roll
            if (credits != null)
            {
                Box(new Rect(0, 0, vw, VH), new Color(0, 0, 0, 0.6f));
                for (int i = 0; i < credits.Length; i++)
                {
                    float y = creditsY + i * 64;
                    if (y < -80 || y > VH + 80) continue;
                    var st = credits[i].StartsWith("#") ? sMid : new GUIStyle(sMid) { fontSize = 36 };
                    Shadowed(new Rect(0, y, vw, 64), credits[i].TrimStart('#'), st, Color.white);
                }
            }

            // fade and flash
            if (fade > 0) Box(new Rect(0, 0, vw, VH), new Color(0, 0, 0, fade));
            if (flash > 0) Box(new Rect(0, 0, vw, VH), new Color(1, 0.97f, 0.9f, Mathf.Clamp01(flash)));

            if (TitleMode && menus.Count > 0) DrawTitle(vw);
            if (menus.Count > 0) DrawMenu(vw, menus.Peek());
        }

        void DrawHud(float vw)
        {
            var st = new GUIStyle(sSmall) { fontSize = 30, fontStyle = FontStyle.Bold };
            Shadowed(new Rect(40, 30, 700, 44), hudEra, st, new Color(1, 1, 1, 0.9f));
            if (hudCharges >= 0)
            {
                Shadowed(new Rect(40, 76, 200, 40), "Tether", sSmall, new Color(1, 0.9f, 0.6f, 0.9f));
                for (int i = 0; i < 3; i++)
                    Box(new Rect(150 + i * 38, 86, 26, 26), i < hudCharges ? new Color(0.5f, 0.95f, 1f, 0.95f) : new Color(1, 1, 1, 0.18f));
            }
            if (hudShift)
                Shadowed(new Rect(vw - 540, 30, 500, 44), GInput.Key("SHIFT") + "  Shift time", new GUIStyle(sSmall) { alignment = TextAnchor.MiddleRight }, new Color(1, 1, 1, 0.6f));
            Shadowed(new Rect(vw - 540, 70, 500, 44), GInput.Key("PAUSE") + "  Pause / Settings", new GUIStyle(sSmall) { alignment = TextAnchor.MiddleRight, fontSize = 24 }, new Color(1, 1, 1, 0.45f));
        }

        void DrawTitle(float vw)
        {
            Shadowed(new Rect(0, 120, vw, 200), "TETHER", new GUIStyle(sBig) { fontSize = 170 }, new Color(1f, 0.93f, 0.78f));
            Shadowed(new Rect(0, 300, vw, 70), "You create a better world without you.", new GUIStyle(sMid) { fontStyle = FontStyle.Italic, fontSize = 40 }, new Color(1, 1, 1, 0.85f));
        }

        void DrawMenu(float vw, Menu m)
        {
            float rowH = 66;
            float w = 980;
            float h = 110 + m.Items.Count * rowH + 70;
            float x = (vw - w) / 2;
            float y = TitleMode && menus.Count == 1 ? 430 : (VH - h) / 2;
            if (m.Opaque || !TitleMode || menus.Count > 1) Box(new Rect(0, 0, vw, VH), new Color(0, 0, 0, 0.55f));
            Box(new Rect(x, y, w, h), new Color(0.05f, 0.05f, 0.08f, 0.88f));
            Box(new Rect(x, y, w, 6), new Color(1f, 0.8f, 0.4f, 0.9f));
            Shadowed(new Rect(x, y + 20, w, 70), m.Title, new GUIStyle(sMid) { fontSize = 48, fontStyle = FontStyle.Bold }, Color.white);

            Event e = Event.current;
            bool moved = (e.mousePosition - lastMouse).sqrMagnitude > 1f;
            lastMouse = e.mousePosition;
            for (int i = 0; i < m.Items.Count; i++)
            {
                var it = m.Items[i];
                var r = new Rect(x + 40, y + 110 + i * rowH, w - 80, rowH - 8);
                if (moved && r.Contains(e.mousePosition) && e.type == EventType.Repaint && m.Sel != i) m.Sel = i;
                bool sel = i == m.Sel;
                if (sel) Box(r, new Color(1f, 0.8f, 0.4f, 0.22f));
                Color c = sel ? new Color(1f, 0.9f, 0.6f) : new Color(1, 1, 1, 0.85f);
                Shadowed(new Rect(r.x + 24, r.y, r.width, r.height), it.Label, sItem, c);
                if (it.Value != null)
                {
                    var vs = new GUIStyle(sItem) { alignment = TextAnchor.MiddleRight, fontSize = 34 };
                    Shadowed(new Rect(r.x, r.y, r.width - 24, r.height), "<  " + it.Value() + "  >", vs, c);
                }
                if (e.type == EventType.MouseDown && r.Contains(e.mousePosition))
                {
                    m.Sel = i;
                    if (it.Value != null && it.Change != null)
                    {
                        bool left = e.mousePosition.x < r.x + r.width * 0.75f && e.mousePosition.x > r.x + r.width * 0.5f;
                        it.Change(left ? -1 : 1);
                        AudioDir.I.Play("move", 0.5f);
                    }
                    else if (it.Confirm != null) { AudioDir.I.Play("confirm", 0.5f); it.Confirm(); }
                    e.Use();
                    return;
                }
            }
            string foot = m.Footer ?? (GInput.LastDevice == Device.Gamepad ? "D-pad / Stick: choose     (A): select     (B): back" : "Arrows / WASD: choose     Enter or Space: select     Esc: back     Mouse works too");
            Shadowed(new Rect(x, y + h - 60, w, 50), foot, new GUIStyle(sSmall) { alignment = TextAnchor.MiddleCenter, fontSize = 24 }, new Color(1, 1, 1, 0.55f));
        }
    }
}
