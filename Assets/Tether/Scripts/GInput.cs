using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Tether
{
    public enum InputMode { Auto, Keyboard, Gamepad }
    public enum Device { Keyboard, Gamepad }

    /// <summary>
    /// Polls keyboard and gamepad once per frame. Gameplay input respects the
    /// device chosen in Settings; menu navigation always accepts both devices so
    /// the player can never lock themselves out of the settings screen.
    /// Works with the new Input System package, and falls back to the legacy
    /// Input Manager when the package is not enabled.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class GInput : MonoBehaviour
    {
        // Gameplay (filtered by Settings.Input)
        public static float MoveX, MoveY;
        public static bool JumpDown, JumpHeld, ShiftDown, InteractDown, PauseDown;
        // Menus (always both devices)
        public static bool ConfirmDown, BackDown, NavUp, NavDown, NavLeft, NavRight;
        public static Device LastDevice = Device.Keyboard;
        public static bool GamepadConnected;

        int navX, navY;
        float navTimer;

        void Update()
        {
            float x = 0, y = 0, mx = 0, my = 0;
            bool jd = false, jh = false, sd = false, id = false, pd = false, cd = false, bd = false;
            bool kbUsed = false, gpUsed = false;
            InputMode mode = Settings.Input;
            bool kbOk = mode != InputMode.Gamepad;
            bool gpOk = mode != InputMode.Keyboard;

#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            var gp = Gamepad.current;
            GamepadConnected = gp != null;
            if (kb != null)
            {
                float kx = 0, ky = 0;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) kx -= 1;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) kx += 1;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) ky += 1;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) ky -= 1;
                bool kJump = kb.spaceKey.wasPressedThisFrame;
                bool kEsc = kb.escapeKey.wasPressedThisFrame;
                mx += kx; my += ky;
                cd |= kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kJump;
                bd |= kEsc || kb.backspaceKey.wasPressedThisFrame;
                if (kbOk)
                {
                    x += kx; y += ky;
                    jd |= kJump; jh |= kb.spaceKey.isPressed;
                    sd |= kb.leftShiftKey.wasPressedThisFrame || kb.rightShiftKey.wasPressedThisFrame || kb.kKey.wasPressedThisFrame;
                    id |= kb.eKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame;
                }
                pd |= kEsc || kb.pKey.wasPressedThisFrame;
                if (kb.anyKey.wasPressedThisFrame) kbUsed = true;
            }
            if (gp != null)
            {
                Vector2 s = gp.leftStick.ReadValue();
                Vector2 d = gp.dpad.ReadValue();
                float gx = Mathf.Abs(s.x) > 0.35f ? Mathf.Sign(s.x) : 0;
                float gy = Mathf.Abs(s.y) > 0.5f ? Mathf.Sign(s.y) : 0;
                if (Mathf.Abs(d.x) > 0.5f) gx = Mathf.Sign(d.x);
                if (Mathf.Abs(d.y) > 0.5f) gy = Mathf.Sign(d.y);
                mx += gx; my += gy;
                bool gJump = gp.buttonSouth.wasPressedThisFrame;
                cd |= gJump;
                bd |= gp.buttonEast.wasPressedThisFrame;
                if (gpOk)
                {
                    x += gx; y += gy;
                    jd |= gJump; jh |= gp.buttonSouth.isPressed;
                    sd |= gp.buttonWest.wasPressedThisFrame || gp.rightShoulder.wasPressedThisFrame || gp.leftShoulder.wasPressedThisFrame;
                    id |= gp.buttonNorth.wasPressedThisFrame;
                }
                pd |= gp.startButton.wasPressedThisFrame;
                if (gJump || gp.buttonEast.wasPressedThisFrame || gp.buttonWest.wasPressedThisFrame ||
                    gp.buttonNorth.wasPressedThisFrame || gp.startButton.wasPressedThisFrame ||
                    s.magnitude > 0.5f || d.magnitude > 0.5f) gpUsed = true;
            }
#else
            string[] names = Input.GetJoystickNames();
            GamepadConnected = false;
            foreach (var n in names) if (!string.IsNullOrEmpty(n)) GamepadConnected = true;
            {
                float kx = 0, ky = 0;
                if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) kx -= 1;
                if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) kx += 1;
                if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) ky += 1;
                if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) ky -= 1;
                bool kJump = Input.GetKeyDown(KeyCode.Space);
                bool kEsc = Input.GetKeyDown(KeyCode.Escape);
                mx += kx; my += ky;
                cd |= Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || kJump;
                bd |= kEsc || Input.GetKeyDown(KeyCode.Backspace);
                if (kbOk)
                {
                    x += kx; y += ky;
                    jd |= kJump; jh |= Input.GetKey(KeyCode.Space);
                    sd |= Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift) || Input.GetKeyDown(KeyCode.K);
                    id |= Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return);
                }
                pd |= kEsc || Input.GetKeyDown(KeyCode.P);
                if (Input.anyKeyDown && !JoyAnyDown()) kbUsed = true;
            }
            {
                float gx = 0, gy = 0;
                try
                {
                    float ax = Input.GetAxisRaw("Horizontal"), ay = Input.GetAxisRaw("Vertical");
                    if (Mathf.Abs(ax) > 0.35f) gx = Mathf.Sign(ax);
                    if (Mathf.Abs(ay) > 0.5f) gy = Mathf.Sign(ay);
                }
                catch (System.Exception) { }
                bool gJump = Input.GetKeyDown(KeyCode.JoystickButton0);
                mx += gx; my += gy;
                cd |= gJump;
                bd |= Input.GetKeyDown(KeyCode.JoystickButton1);
                if (gpOk)
                {
                    x += gx; y += gy;
                    jd |= gJump; jh |= Input.GetKey(KeyCode.JoystickButton0);
                    sd |= Input.GetKeyDown(KeyCode.JoystickButton2) || Input.GetKeyDown(KeyCode.JoystickButton4) || Input.GetKeyDown(KeyCode.JoystickButton5);
                    id |= Input.GetKeyDown(KeyCode.JoystickButton3);
                }
                pd |= Input.GetKeyDown(KeyCode.JoystickButton7);
                if (JoyAnyDown()) gpUsed = true;
            }
#endif
            if (gpUsed) LastDevice = Device.Gamepad;
            else if (kbUsed) LastDevice = Device.Keyboard;
            if (mode == InputMode.Keyboard) LastDevice = Device.Keyboard;
            if (mode == InputMode.Gamepad) LastDevice = Device.Gamepad;

            MoveX = Mathf.Clamp(x, -1, 1);
            MoveY = Mathf.Clamp(y, -1, 1);
            JumpDown = jd; JumpHeld = jh; ShiftDown = sd; InteractDown = id; PauseDown = pd;
            ConfirmDown = cd; BackDown = bd;

            // Menu navigation with key repeat
            int nx = mx > 0.5f ? 1 : mx < -0.5f ? -1 : 0;
            int ny = my > 0.5f ? 1 : my < -0.5f ? -1 : 0;
            NavUp = NavDown = NavLeft = NavRight = false;
            bool fire = false;
            if (nx != navX || ny != navY)
            {
                navX = nx; navY = ny;
                fire = nx != 0 || ny != 0;
                navTimer = 0.38f;
            }
            else if (nx != 0 || ny != 0)
            {
                navTimer -= Time.unscaledDeltaTime;
                if (navTimer <= 0) { fire = true; navTimer = 0.11f; }
            }
            if (fire)
            {
                NavUp = ny > 0; NavDown = ny < 0;
                NavLeft = nx < 0; NavRight = nx > 0;
            }
        }

#if !ENABLE_INPUT_SYSTEM
        static bool JoyAnyDown()
        {
            for (int i = 0; i < 10; i++)
                if (Input.GetKeyDown(KeyCode.JoystickButton0 + i)) return true;
            return false;
        }
#endif

        public static void Consume()
        {
            JumpDown = ShiftDown = InteractDown = ConfirmDown = BackDown = PauseDown = false;
            NavUp = NavDown = NavLeft = NavRight = false;
        }

        public static string Key(string action)
        {
            bool pad = LastDevice == Device.Gamepad;
            switch (action)
            {
                case "MOVE": return pad ? "Left Stick" : "A / D";
                case "JUMP": return pad ? "(A)" : "Space";
                case "SHIFT": return pad ? "(X)" : "Shift";
                case "INTERACT": return pad ? "(Y)" : "E";
                case "UP": return pad ? "Up" : "W";
                case "DOWN": return pad ? "Down" : "S";
                case "PAUSE": return pad ? "Start" : "Esc";
                case "CONFIRM": return pad ? "(A)" : "Space";
            }
            return action;
        }

        public static string Fill(string text)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf('{') < 0) return text;
            foreach (var k in new[] { "MOVE", "JUMP", "SHIFT", "INTERACT", "UP", "DOWN", "PAUSE", "CONFIRM" })
                text = text.Replace("{" + k + "}", Key(k));
            return text;
        }
    }
}
