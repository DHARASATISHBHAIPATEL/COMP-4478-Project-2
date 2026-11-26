using System.Collections;
using UnityEngine;

namespace Tether
{
    /// <summary>
    /// Entry point. Creates every system at runtime, so the game runs from any
    /// scene (even an empty one): press Play and the title screen appears.
    /// Handles the title menu, chapter flow, pause and the settings screen.
    /// </summary>
    public class Game : MonoBehaviour
    {
        public static Game I;
        public bool Paused;

        LevelRuntime level;
        LevelPlan[] plans;
        Coroutine flow;
        float titleX;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (I != null) return;
            new GameObject("Tether Game").AddComponent<Game>();
        }

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);

            Time.timeScale = 1f;
            Time.fixedDeltaTime = 1f / 60f;
            QualitySettings.vSyncCount = 1;
            Physics2D.queriesHitTriggers = false;

            Settings.Load();
            if (PlayerPrefs.HasKey("t_res")) Settings.ApplyAll();

            gameObject.AddComponent<GInput>();
            gameObject.AddComponent<AudioDir>();
            gameObject.AddComponent<UI>();

            var cam = Camera.main;
            if (cam == null)
            {
                var cgo = new GameObject("Main Camera");
                cgo.tag = "MainCamera";
                cam = cgo.AddComponent<Camera>();
            }
            DontDestroyOnLoad(cam.gameObject);
            if (cam.GetComponent<AudioListener>() == null) cam.gameObject.AddComponent<AudioListener>();
            cam.gameObject.AddComponent<Cam>();
            cam.gameObject.AddComponent<Backdrop>().Init(cam);
        }

        void Start()
        {
            flow = StartCoroutine(Title());
        }

        // ---------------- flow ----------------

        void DestroyLevel()
        {
            if (level != null) Destroy(level.gameObject);
            level = null;
            LevelRuntime.I = null;
            Player.I = null;
        }

        IEnumerator Title()
        {
            UI.I.SetFade(1);
            DestroyLevel();
            UI.I.ResetOverlays();
            UI.I.TitleMode = true;
            Cam.I.ClearBounds();
            titleX = 0;
            Cam.I.Hold(new Vector2(0, 4), 6.2f);
            Backdrop.I.Set(Era.Future, false);
            AudioDir.I.SetLoop(null, 0);
            AudioDir.I.PlayMusic("theme");
            UI.I.CloseAllMenus();
            UI.I.OpenMenu(TitleMenu());
            yield return UI.I.FadeTo(0, 1.5f);
        }

        Menu TitleMenu()
        {
            var m = new Menu { Title = "" };
            m.Items.Add(new MenuItem("Start", () => StartGame(0)));
            m.Items.Add(new MenuItem("Chapters", () => UI.I.OpenMenu(ChapterMenu())));
            m.Items.Add(new MenuItem("Settings", () => UI.I.OpenMenu(SettingsMenu())));
            m.Items.Add(new MenuItem("Quit", () => Application.Quit()));
            m.Title = "Main Menu";
            return m;
        }

        Menu ChapterMenu()
        {
            var m = new Menu { Title = "Chapters" };
            var all = Levels.All();
            for (int i = 0; i < all.Length; i++)
            {
                int idx = i;
                m.Items.Add(new MenuItem(all[i].Chapter + "   " + all[i].Title, () => StartGame(idx)));
            }
            m.Items.Add(new MenuItem("Back", () => UI.I.CloseMenu()));
            m.OnBack = () => UI.I.CloseMenu();
            return m;
        }

        public Menu SettingsMenu()
        {
            Settings.RefreshLists();
            var m = new Menu { Title = "Settings" };
            m.Items.Add(new MenuItem("Display", Settings.DisplayName, d =>
            {
                Settings.RefreshLists();
                int n = Mathf.Max(1, Settings.Displays.Count);
                Settings.DisplayIndex = (Settings.DisplayIndex + d + n) % n;
                Settings.ApplyDisplay(); Settings.Save();
            }));
            m.Items.Add(new MenuItem("Resolution", Settings.ResolutionName, d =>
            {
                int n = Settings.Resolutions.Count;
                Settings.ResIndex = (Mathf.Max(0, Settings.ResIndex) + d + n) % n;
                Settings.ApplyResolution(); Settings.Save();
            }));
            m.Items.Add(new MenuItem("Window Mode", () => Settings.ModeNames[Settings.ModeIndex], d =>
            {
                int n = Settings.Modes.Length;
                Settings.ModeIndex = (Settings.ModeIndex + d + n) % n;
                Settings.ApplyResolution(); Settings.Save();
            }));
            m.Items.Add(new MenuItem("Input Device", Settings.InputName, d =>
            {
                Settings.Input = (InputMode)(((int)Settings.Input + d + 3) % 3);
                Settings.Save();
            }));
            m.Items.Add(new MenuItem("Music Volume", () => Bar(Settings.Music), d =>
            {
                Settings.Music = Mathf.Clamp01(Mathf.Round(Settings.Music * 10 + d) / 10f); Settings.Save();
            }));
            m.Items.Add(new MenuItem("Sound Effects", () => Bar(Settings.Sfx), d =>
            {
                Settings.Sfx = Mathf.Clamp01(Mathf.Round(Settings.Sfx * 10 + d) / 10f); Settings.Save();
                AudioDir.I.Play("confirm", 0.8f);
            }));
            m.Items.Add(new MenuItem("Back", () => UI.I.CloseMenu()));
            m.OnBack = () => UI.I.CloseMenu();
            m.Footer = null;
            return m;
        }

        static string Bar(float v)
        {
            int n = Mathf.RoundToInt(v * 10);
            return new string('|', n) + new string('.', 10 - n);
        }

        Menu PauseMenu()
        {
            var m = new Menu { Title = "Paused" };
            m.Items.Add(new MenuItem("Resume", Resume));
            m.Items.Add(new MenuItem("Restart from Checkpoint", () => { Resume(); if (level != null) level.Kill(); }));
            m.Items.Add(new MenuItem("Settings", () => UI.I.OpenMenu(SettingsMenu())));
            m.Items.Add(new MenuItem("Quit to Title", () => { Resume(); BackToTitle(); }));
            m.OnBack = Resume;
            return m;
        }

        public void StartGame(int index)
        {
            UI.I.CloseAllMenus();
            UI.I.TitleMode = false;
            if (flow != null) StopCoroutine(flow);
            flow = StartCoroutine(Run(index));
        }

        void BackToTitle()
        {
            if (flow != null) StopCoroutine(flow);
            flow = StartCoroutine(Title());
        }

        IEnumerator Run(int start)
        {
            plans = Levels.All();
            for (int i = start; i < plans.Length; i++)
            {
                yield return UI.I.FadeTo(1, 0.6f);
                DestroyLevel();
                UI.I.ResetOverlays();
                yield return null;
                var p = Levels.All()[i]; // fresh plan each time
                level = LevelRuntime.Build(p);
                level.Busy = true;
                level.player.Frozen = true;
                StartCoroutine(UI.I.FadeTo(0, 1.0f));
                if (p.Intro != null) yield return p.Intro(level);
                if (level == null) yield break;
                level.Busy = false;
                level.player.Frozen = false;
                level.player.PoseOverride = null;
                UI.I.LetterboxTarget = 0;
                Cam.I.Follow(level.player.transform, false);

                while (level != null && !level.Finished) yield return null;
                if (level == null) yield break;
                level.Busy = true;
                level.player.Frozen = true;
                if (p.Outro != null) yield return p.Outro(level);
                PlayerPrefs.SetInt("t_unlocked", Mathf.Max(PlayerPrefs.GetInt("t_unlocked", 0), i + 1));
            }
            BackToTitle();
        }

        public void Resume()
        {
            UI.I.CloseAllMenus();
            Paused = false;
            Time.timeScale = 1f;
            GInput.Consume();
        }

        void Update()
        {
            if (UI.I.TitleMode)
            {
                titleX += Time.unscaledDeltaTime * 0.8f;
                Cam.I.Hold(new Vector2(titleX, 4), 6.2f);
                return;
            }
            if (level != null && !Paused && !UI.I.MenuOpen && GInput.PauseDown && !level.Finished)
            {
                Paused = true;
                Time.timeScale = 0f;
                UI.I.OpenMenu(PauseMenu());
            }
        }
    }
}
