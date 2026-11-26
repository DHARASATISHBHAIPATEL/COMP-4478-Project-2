using System.Collections.Generic;
using UnityEngine;

namespace Tether
{
    /// <summary>Player options. Every option applies immediately and is saved in PlayerPrefs.</summary>
    public static class Settings
    {
        public static float Music = 0.7f;
        public static float Sfx = 0.8f;
        public static InputMode Input = InputMode.Auto;
        public static int ResIndex = -1;
        public static int ModeIndex = 0;
        public static int DisplayIndex = 0;

        public static readonly List<Vector2Int> Resolutions = new List<Vector2Int>();
        public static readonly List<DisplayInfo> Displays = new List<DisplayInfo>();

        public static readonly FullScreenMode[] Modes =
        {
            FullScreenMode.FullScreenWindow, FullScreenMode.ExclusiveFullScreen, FullScreenMode.Windowed
        };
        public static readonly string[] ModeNames = { "Borderless Fullscreen", "Exclusive Fullscreen", "Windowed" };

        public static void Load()
        {
            Music = PlayerPrefs.GetFloat("t_music", 0.7f);
            Sfx = PlayerPrefs.GetFloat("t_sfx", 0.8f);
            Input = (InputMode)PlayerPrefs.GetInt("t_input", 0);
            ModeIndex = Mathf.Clamp(PlayerPrefs.GetInt("t_mode", ModeFromScreen()), 0, Modes.Length - 1);
            DisplayIndex = PlayerPrefs.GetInt("t_display", 0);
            RefreshLists();
            int saved = PlayerPrefs.GetInt("t_res", -1);
            ResIndex = saved >= 0 && saved < Resolutions.Count ? saved : CurrentResIndex();
        }

        public static void Save()
        {
            PlayerPrefs.SetFloat("t_music", Music);
            PlayerPrefs.SetFloat("t_sfx", Sfx);
            PlayerPrefs.SetInt("t_input", (int)Input);
            PlayerPrefs.SetInt("t_mode", ModeIndex);
            PlayerPrefs.SetInt("t_res", ResIndex);
            PlayerPrefs.SetInt("t_display", DisplayIndex);
            PlayerPrefs.Save();
        }

        static int ModeFromScreen()
        {
            for (int i = 0; i < Modes.Length; i++) if (Modes[i] == Screen.fullScreenMode) return i;
            return 0;
        }

        public static void RefreshLists()
        {
            Resolutions.Clear();
            foreach (var r in Screen.resolutions)
            {
                var v = new Vector2Int(r.width, r.height);
                if (!Resolutions.Contains(v)) Resolutions.Add(v);
            }
            if (Resolutions.Count == 0) Resolutions.Add(new Vector2Int(Screen.width, Screen.height));
            Resolutions.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));

            Displays.Clear();
            try { Screen.GetDisplayLayout(Displays); } catch (System.Exception) { }
            if (DisplayIndex >= Displays.Count) DisplayIndex = 0;
        }

        static int CurrentResIndex()
        {
            var cur = new Vector2Int(Screen.width, Screen.height);
            int idx = Resolutions.IndexOf(cur);
            return idx >= 0 ? idx : Resolutions.Count - 1;
        }

        public static string ResolutionName()
        {
            if (ResIndex < 0 || ResIndex >= Resolutions.Count) return Screen.width + " x " + Screen.height;
            var r = Resolutions[ResIndex];
            return r.x + " x " + r.y;
        }

        public static string DisplayName()
        {
            if (Displays.Count == 0) return "Display 1";
            var d = Displays[Mathf.Clamp(DisplayIndex, 0, Displays.Count - 1)];
            string n = string.IsNullOrEmpty(d.name) ? "Display" : d.name;
            return (DisplayIndex + 1) + ": " + n + " (" + d.width + "x" + d.height + ")";
        }

        public static string InputName()
        {
            switch (Input)
            {
                case InputMode.Keyboard: return "Keyboard only";
                case InputMode.Gamepad: return GInput.GamepadConnected ? "Controller only" : "Controller only (none found)";
                default: return "Keyboard + Controller";
            }
        }

        public static void ApplyResolution()
        {
            if (ResIndex < 0 || ResIndex >= Resolutions.Count) return;
            var r = Resolutions[ResIndex];
            Screen.SetResolution(r.x, r.y, Modes[ModeIndex]);
        }

        public static void ApplyDisplay()
        {
#if !UNITY_EDITOR
            if (Displays.Count == 0) return;
            var d = Displays[Mathf.Clamp(DisplayIndex, 0, Displays.Count - 1)];
            var pos = new Vector2Int(Mathf.Max(0, (d.width - Screen.width) / 2), Mathf.Max(0, (d.height - Screen.height) / 2));
            Screen.MoveMainWindowTo(d, pos);
#endif
        }

        public static void ApplyAll()
        {
            ApplyDisplay();
            ApplyResolution();
        }
    }
}
