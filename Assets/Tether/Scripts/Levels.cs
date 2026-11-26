using System.Collections;
using UnityEngine;

namespace Tether
{
    /// <summary>
    /// The five chapters and the epilogue: layouts, story text and cutscenes.
    /// Coordinates are grid cells: x to the right, y up, ground surfaces at the
    /// "top" value given to Ground().
    /// </summary>
    public static class Levels
    {
        static readonly Color IvyTint = new Color(0.86f, 0.84f, 0.82f);
        static readonly Color InesTint = Color.white;

        public static LevelPlan[] All()
        {
            return new[] { Ashfall(), TwoCities(), Lantern(), DeepTime(), LastShift(), Epilogue() };
        }

        // =====================================================================
        // Chapter 1: Ashfall (2190 only)
        // =====================================================================
        static LevelPlan Ashfall()
        {
            var p = new LevelPlan("Ashfall", "Chapter 1", Era.Future, Era.Modern, 130, 22);
            p.ShiftEnabled = false;
            p.MusicA = "future";
            p.Start = new Vector2(3, 6);

            // rooftops (tutorial)
            p.Ground(0, 12, 6);
            p.Text(4, 6, "Move with {MOVE}.", null, false, 3);
            p.Text(10.5f, 6, "Jump with {JUMP}. Hold it to jump higher.", null, false, 2);
            p.Ground(15, 8, 7);
            p.Ground(26, 6, 5);
            p.Ground(34, 5, 7);
            p.Text(29, 5, "Fall between the roofs and the ash takes you back to the last lantern.", null, false, 2);

            // Ivy's camp
            p.Ground(39, 22, 4);
            p.Checkpoint(41, 4);
            p.Prop("fire", 44, 4, EM.A);
            p.Npc("ivy", "Female", "Ivy", 48, 4, EM.A, true, IvyTint);
            p.Cue("ivy", 45, IvyTalk);
            p.Text(53, 4, "A mural of a tower pouring light over the city. Painted words: THE LIGHT THAT ATE THE CITY.");
            p.Text(58.5f, 4, "Ivy's sign: HOLLOWS BELOW. STAY HIGH.", null, true, 2);

            // Street of Hollows
            p.Ground(61, 64, 2);
            p.Block(61, 5, 5, 1);
            p.Text(62.5f, 6, "Hollows hunt by sight. Stay above them, or outrun them.", null, false, 2.5f);
            p.Block(69, 5, 4, 1);
            p.Block(76, 6, 4, 1);
            p.Block(83, 5, 4, 1);
            p.Block(90, 6, 3, 1);
            p.Hollow(72, 2, 63, 81);
            p.Hollow(88, 2, 82, 98);

            // Vale tower
            p.Block(100, 2, 1, 1);           // door step (Hollows can't climb it)
            p.Block(100, 6, 1, 16);          // left wall above the door
            p.Block(125, 2, 1, 20);          // right wall
            p.Checkpoint(103, 2);
            p.Text(102, 2, "Climb ladders with {UP} and {DOWN}.", null, false, 2);
            p.Ladder(105, 2, 5);
            p.Block(106, 6, 6, 1);
            p.Crumble(112, 6, 4);
            p.Block(116, 6, 9, 1);
            p.Text(109, 7, "A dusty portrait: OTTO BRASK, WATCHMAKER, 1889. His crest is a clock with one broken hand.");
            p.Checkpoint(119, 7);
            p.Ladder(123, 7, 6);
            p.Block(101, 12, 10, 1);
            p.Crumble(111, 12, 5);
            p.Block(116, 12, 7, 1);
            p.Text(118, 13, "The floor is rotten. Keep moving.", null, false, 2);
            p.TetherPickup(103, 13.8f);
            p.Cues["tether"] = TetherFound;

            p.Intro = L => Opening(L);
            return p;
        }

        static IEnumerator Opening(LevelRuntime L)
        {
            UI.I.LetterboxTarget = 1;
            Cam.I.Hold(new Vector2(62, 12), 11f);
            L.StartCoroutine(Cam.I.PanTo(new Vector2(6, 8), 6.2f, 9f));
            yield return UI.I.Card("TETHER", "", 1.6f);
            yield return UI.I.Card("Halden, 2190", "One hundred and fifty years after the Bloom.", 2.2f);
            yield return new WaitForSeconds(0.6f);
            yield return UI.I.Card("Chapter 1", "Ashfall", 1.2f);
        }

        static IEnumerator IvyTalk(LevelRuntime L, Ent e)
        {
            var ivy = L.NpcById("ivy");
            yield return L.Letterbox(true);
            yield return L.WalkTo(45.6f);
            L.StartCoroutine(Cam.I.PanTo(new Vector2(47, 6), 4.5f, 1.2f));
            yield return L.Say("Ivy", "Wren. You made it back from the east roofs. Good.", ivy);
            yield return L.Say("Ivy", "The old Vale tower started humming last night. Same hum I heard the night of the Bloom. The night I lost my arm.", ivy);
            yield return L.Say("Ivy", "Whatever is making that sound, bring it to me. Take the high road over the street.", ivy);
            yield return L.Say("Ivy", "The Hollows down there used to be people. They don't remember that.", ivy);
            yield return L.Letterbox(false);
        }

        static IEnumerator TetherFound(LevelRuntime L, Ent e)
        {
            yield return L.Letterbox(true);
            L.player.PoseOverride = "hold1";
            AudioDir.I.Play("pickup", 0.8f);
            yield return Cam.I.PanTo(L.player.Center + new Vector2(0, 0.5f), 3.5f, 1.2f);
            yield return L.Say("", "A pocket device, warm as a hand. On the lid, a crest: a clock with one broken hand.");
            yield return L.Say("", "It ticks once. Then the whole world ticks with it.");
            AudioDir.I.Play("flare", 0.8f);
            Cam.I.Shake(0.4f);
            UI.I.Flash(1f);
            Backdrop.I.Set(Era.Modern, false);
            yield return new WaitForSeconds(0.7f);
            UI.I.Flash(1f);
            Backdrop.I.Set(Era.Future, false);
            AudioDir.I.Play("shift", 0.8f);
            yield return UI.I.Card("", "It remembers.", 1.6f);
            yield return UI.I.FadeTo(1, 1f);
            L.Finish();
        }

        // =====================================================================
        // Chapter 2: Two Cities (2190 <-> 2041)
        // =====================================================================
        static LevelPlan TwoCities()
        {
            var p = new LevelPlan("Two Cities", "Chapter 2", Era.Future, Era.Modern, 150, 20);
            p.MusicA = "future"; p.MusicB = "modern";
            p.Start = new Vector2(2, 4);

            p.Ground(0, 14, 4);
            p.Checkpoint(4, 4);
            p.Text(10, 4, "The bridge is gone. The Tether hums in your hand. Press {SHIFT} to Shift time.",
                          "In 2041 the bridge is new. Press {SHIFT} again to Shift back.", false, 4);
            // bridge exists only in 2041
            p.Block(14, 3, 12, 1, EM.B);
            p.Block(14, 3, 2, 1, EM.Both);
            p.Block(24, 3, 2, 1, EM.Both);

            p.Ground(26, 44, 4);
            p.Checkpoint(28, 4);
            p.Text(35, 4, "A faded poster: LANTERN ENERGY. POWER FOR A THOUSAND YEARS. Someone has written over it: LIARS.",
                          "A new poster: LANTERN ENERGY. POWER FOR A THOUSAND YEARS. Opening night this Friday!");
            // flood wall: whole in 2041, rubble in 2190
            p.Block(44, 4, 2, 7, EM.B);
            p.Block(44, 4, 2, 1, EM.Both);
            p.Text(41, 4, "The old flood wall has crumbled. You can climb over the rubble.",
                          "The flood wall is brand new. There is no way through in this time.", false, 2.5f);
            // balcony only in 2041
            p.Block(55, 5, 3, 1, EM.B);
            p.Text(53, 4, "A balcony stood here once. Its ghost hangs in the air. Shift to use it.",
                          null, false, 2.5f);
            p.Ground(59, 24, 8);
            p.Checkpoint(61, 8);

            // scaffold climb: ledges from both times
            p.Text(71, 8, "Ledges from both times. Jump, then Shift in mid-air to land on the other time's ledge.",
                          "Ledges from both times. Jump, then Shift in mid-air to land on the other time's ledge.", false, 3);
            p.Block(74, 9, 3, 1, EM.A);
            p.Block(78, 11, 3, 1, EM.B);
            p.Block(74, 13, 3, 1, EM.A);
            p.Block(78, 15, 3, 1, EM.B);
            p.Block(82, 15, 10, 1);
            p.Block(82, 8, 1, 7);                // wall under the rooftop: the only way on is up
            p.Checkpoint(85, 16);

            // mirrored street
            p.Ground(83, 50, 4);
            p.Checkpoint(95, 4);
            p.Text(98, 4, "Ash Patrol notice: STAY OFF THE STREETS AFTER DARK.",
                          "Lantern Security: KEEP MOVING, CITIZEN. CURFEW AT 10.");
            p.Hollow(105, 4, 100, 113);
            p.Hollow(120, 4, 115, 127);
            p.Guard(108, 4, 101, 113, EM.B);
            p.Guard(124, 4, 118, 128, EM.B);
            p.Shadow(114, 4, 3, 3);
            p.Text(112.5f, 4, "Guards can't see you in the shadows. Hollows can't hear a Shift.",
                              "Guards can't see you in the shadows. Hollows can't hear a Shift.", false, 2);

            // Lantern gate
            p.Ground(133, 17, 4);
            p.Checkpoint(131, 4);
            p.Prop("window", 142, 5.5f, EM.B, 2f);
            p.Npc("ines", "Female", "Dr. Ines Vale", 142, 4, EM.B, true, InesTint);
            p.Prop("crystal", 147, 4, EM.Both, 1.6f, new Color(1, 1, 1, 0.9f));
            p.Cue("gate", 136, GateScene);

            p.Intro = L => ChapterIntro(L, "Chapter 2", "Two Cities",
                "The Tether pulls between two moments of the same place: 2190 and 2041.");
            return p;
        }

        static IEnumerator ChapterIntro(LevelRuntime L, string chapter, string title, string line)
        {
            UI.I.LetterboxTarget = 1;
            yield return UI.I.Card(chapter, title, 1.4f);
            if (line != null) yield return L.Say("", line);
            UI.I.LetterboxTarget = 0;
        }

        static IEnumerator GateScene(LevelRuntime L, Ent e)
        {
            yield return L.Letterbox(true);
            yield return L.WalkTo(138f);
            if (L.InA)
            {
                yield return L.Say("", "The Tether tugs at Wren's hand, hard.");
                L.ForceEra(false);
                yield return new WaitForSeconds(0.6f);
            }
            var ines = L.NpcById("ines");
            yield return Cam.I.PanTo(new Vector2(141, 7), 4.5f, 1.4f);
            yield return L.Say("Dr. Ines Vale", "Citizens of Halden! Tonight the Lantern reaches full power.", ines);
            yield return L.Say("Dr. Ines Vale", "Clean light, for a thousand years. My family will see a sky without smoke.", ines);
            AudioDir.I.Play("flare", 0.9f);
            Cam.I.Shake(0.5f);
            yield return Cam.I.PanTo(new Vector2(141, 9), 7.5f, 0.8f);
            for (int i = 0; i < 7; i++)
            {
                L.ForceEra(!L.InA);
                yield return new WaitForSeconds(0.32f - i * 0.03f);
            }
            if (!L.InA) L.ForceEra(true);
            yield return UI.I.Card("", "The same plaza. Whole. Ruined. Whole.", 1.6f);
            yield return L.Say("", "The light came from the Lantern. Wren follows it down, inside.");
            yield return UI.I.FadeTo(1, 1f);
            L.Finish();
        }

        // =====================================================================
        // Chapter 3: The Lantern (2041 <-> 1889)
        // =====================================================================
        static LevelPlan Lantern()
        {
            var p = new LevelPlan("The Lantern", "Chapter 3", Era.Modern, Era.Past, 135, 24);
            p.MusicA = "modern"; p.MusicB = "past";
            p.Start = new Vector2(2, 3);

            // lobby: guards in 2041, rocks in 1889
            p.Ground(0, 48, 3);
            p.Checkpoint(3, 3);
            p.Text(6, 3, "A plaque: LANTERN TOWER. BUILT 2039 ON THE SITE OF THE OLD BRASK MINE.",
                         "A wooden sign: BRASK & SONS CRYSTAL WORKS. EST. 1889.");
            p.Guard(15, 3, 10, 19, EM.A);
            p.Shadow(21, 3, 3, 3);
            p.Block(24, 3, 2, 5, EM.B);
            p.Shadow(30, 3, 3, 3);
            p.Block(34, 3, 2, 5, EM.B);
            p.Guard(33, 3, 27, 37, EM.A);
            p.Text(9, 3, "Security guards in 2041. Nobody yet in 1889, but the mine yard has rock piles.",
                         "Security guards in 2041. Nobody yet in 1889, but the mine yard has rock piles.", false, 2);
            p.Prop("desk", 18, 3, EM.A);
            p.Prop("desk", 29, 3, EM.A);

            // shaft: smooth steel in 2041, a wooden ladder in 1889
            p.Block(47, 3, 1, 13);
            p.Text(42, 3, "An elevator shaft of smooth steel. No way up in this time.",
                          "An old mine shaft with a wooden ladder.", false, 2.5f);
            p.Ladder(43, 3, 14, EM.B);

            // Ines's office (the building mass below it is solid)
            p.Ground(48, 23, 16);
            p.Block(44, 16, 27, 1);
            p.Checkpoint(46, 17);
            p.Prop("desk", 52, 17, EM.A); p.Prop("desk", 58, 17, EM.A); p.Prop("desk", 64, 17, EM.A);
            p.Text(52, 17, "A sample label: CORE CRYSTAL. SOURCE: BRASK VEIN, DUG 1889.",
                           "Miners' graffiti scratched in the rock: THE STONE SINGS AT NIGHT.");
            p.Text(58, 17, "A test log: 70 percent, stable. 85 percent, stable. The board wants 100 percent by opening night.", null);
            p.Text(64, 17, "An unsent letter from Ines: If we push past 90 percent, I can't promise anything. My daughter deserves a sky.", null);

            // core hall: the core pulses the era every few seconds
            p.Ground(71, 50, 3);
            p.Spikes(71, 3, 46);
            p.Block(71, 10, 5, 1);
            p.Block(76, 10, 8, 1, EM.A);
            p.Block(84, 10, 2, 1);
            p.Block(86, 10, 8, 1, EM.B);
            p.Block(94, 10, 2, 1);
            p.Block(96, 10, 8, 1, EM.A);
            p.Block(104, 10, 2, 1);
            p.Block(106, 10, 8, 1, EM.B);
            p.Block(114, 10, 3, 1);
            p.Ground(117, 18, 11);
            p.Checkpoint(73, 11);
            p.Text(73.5f, 11, "The core pulses, and the Tether answers it. Wait where both times hold you, then run.",
                              "The core pulses, and the Tether answers it. Wait where both times hold you, then run.", false, 2.5f);
            p.Prop("glow", 95, 6, EM.Both, 6f, new Color(0.5f, 0.95f, 1f, 0.35f));
            p.Prop("crystal", 95, 3.5f, EM.Both, 2.4f);
            p.PulseX0 = 71; p.PulseX1 = 116.5f; p.PulseInterval = 2.8f;

            // the lever
            p.Checkpoint(119, 11);
            p.Npc("ines", "Female", "Dr. Ines Vale", 129, 11, EM.A, true, InesTint);
            p.Prop("lever", 131, 11, EM.A, 1.5f);
            p.Cue("lever", 123, LeverScene);

            p.Intro = L => ChapterIntro(L, "Chapter 3", "The Lantern",
                "Inside the Lantern the Tether reaches further back: 2041 and 1889. The ruins of 2190 are out of reach now.");
            return p;
        }

        static IEnumerator LeverScene(LevelRuntime L, Ent e)
        {
            yield return L.Letterbox(true);
            if (!L.InA) { L.ForceEra(true); yield return new WaitForSeconds(0.5f); }
            yield return L.WalkTo(124f);
            var ines = L.NpcById("ines");
            ines.Face(false);
            yield return Cam.I.PanTo(new Vector2(128, 13), 4.8f, 1.2f);
            yield return L.Say("", "Through the glass, Ines stands at the main lever.");
            ines.Pose = "action1";
            yield return L.Say("Dr. Ines Vale", "Ninety percent... ninety-five...", null);
            AudioDir.I.Play("alert", 0.9f);
            AudioDir.I.Play("hum", 1f);
            Cam.I.Shake(0.3f);
            yield return L.Say("Speaker", "CORE AT ONE HUNDRED PERCENT.");
            ines.Pose = "hurt";
            ines.Face(true);
            yield return L.Say("Dr. Ines Vale", "No. No, no, no. Shut it down! SHUT IT DOWN!", null);
            AudioDir.I.Play("flare", 1f);
            for (int i = 0; i < 4; i++) { Cam.I.Shake(0.6f); UI.I.Flash(0.6f); yield return new WaitForSeconds(0.25f); }
            UI.I.Flash(1.2f);
            AudioDir.I.FadeOutMusic();
            yield return UI.I.FadeTo(1, 0.6f);
            AudioDir.I.Play("hum", 0.6f);
            yield return new WaitForSeconds(1.2f);
            yield return UI.I.Card("", "Forward is gone. Only back.", 2f);
            L.Finish();
        }

        // =====================================================================
        // Chapter 4: Deep Time (1889 <-> 2041, limited Tether charges)
        // =====================================================================
        static LevelPlan DeepTime()
        {
            var p = new LevelPlan("Deep Time", "Chapter 4", Era.Past, Era.Modern, 165, 20);
            p.MusicA = "past"; p.MusicB = "modern";
            p.Underground = true;
            p.Charges = 0;
            p.Start = new Vector2(2, 4);

            // mining town
            p.Ground(0, 36, 4);
            p.Checkpoint(3, 4);
            p.Text(8, 4, "Town sign: BRASK HOLLOW. POPULATION 212.", "A concrete wall: LANTERN SUBSTATION 4. DANGER.");
            p.Text(15, 4, "A notice: CRYSTAL PAYS 3 DOLLARS A POUND. DIG DEEP, DIG FAST.", null);
            p.Prop("lantern", 20, 4, EM.A);
            p.Prop("desk", 31, 4, EM.A, 1f, new Color(0.7f, 0.5f, 0.35f));
            p.Npc("otto", "Adventurer", "Otto Brask", 28, 4, EM.A, true);
            p.Cue("otto", 25, OttoScene);

            // gas tunnels
            p.Ground(36, 55, 4);
            p.Block(36, 8, 55, 12);
            p.Checkpoint(40, 4);
            p.Text(38.5f, 4, "The air turns green in pulses. Breathe it and you fall. Time your run, or Shift away from it.",
                             "In 2041 this tunnel is a sealed concrete pipe. No gas, but bulkheads block the way.", false, 2.5f);
            p.Gas(44, 4, 6, 4, 0f);
            p.Gas(58, 4, 6, 4, 1.5f);
            p.Gas(72, 4, 6, 4, 3.0f);
            p.Block(51, 4, 1, 4, EM.B);
            p.Block(65, 4, 1, 4, EM.B);
            p.Block(79, 4, 1, 4, EM.B);
            p.Lamp(55, 4);
            p.Lamp(69, 4);
            p.Checkpoint(67, 4);

            // unstable timbers
            p.Ground(91, 8, 4);
            p.Crumble(99, 3, 3);
            p.Ground(102, 10, 4);
            p.Crumble(112, 3, 3);
            p.Ground(115, 16, 4);
            p.Block(91, 12, 40, 8);
            p.Block(119, 5, 5, 7);
            p.Checkpoint(93, 4);
            p.Checkpoint(116, 4);
            p.Beam(96.5f, 11.4f); p.Beam(106f, 11.4f); p.Beam(109.5f, 11.4f); p.Beam(127f, 11.4f);
            p.Text(94, 4, "The timbers groan. Beams fall when you pass under them. Keep running.", null, false, 2);
            p.Text(117, 4, "The tunnel drops low. Hold {DOWN} to crawl, or run and press {DOWN} to slide.",
                           "The tunnel drops low. Hold {DOWN} to crawl, or run and press {DOWN} to slide.", false, 2.5f);

            // the vein
            p.Ground(131, 34, 4);
            p.Block(131, 14, 34, 6);
            p.Prop("glow", 152, 8, EM.A, 6.5f, new Color(0.5f, 0.95f, 1f, 0.4f));
            p.Prop("crystal", 152, 4, EM.A, 3.2f);
            p.Npc("miner1", "Soldier", "Miner", 147, 4, EM.A, false, new Color(0.8f, 0.65f, 0.5f));
            p.Npc("miner2", "Soldier", "Miner", 157, 4, EM.A, true, new Color(0.8f, 0.65f, 0.5f));
            p.Npc("otto2", "Adventurer", "Otto Brask", -6, 4, EM.A, false);
            p.Text(140, 4, null, "Concrete foundations: LANTERN TOWER, LEVEL B4.", false, 3);
            p.Cue("vein", 141, VeinScene);

            p.Intro = L => ChapterIntro(L, "Chapter 4", "Deep Time",
                "1889. The Tether flickers, but it will not Shift. It needs mending.");
            return p;
        }

        static IEnumerator NpcWalk(Npc n, float x, float speed = 2.5f)
        {
            float t = 0;
            n.Face(x < n.transform.position.x);
            while (Mathf.Abs(n.transform.position.x - x) > 0.05f)
            {
                t += Time.deltaTime;
                var pos = n.transform.position;
                pos.x = Mathf.MoveTowards(pos.x, x, speed * Time.deltaTime);
                n.transform.position = pos;
                n.Pose = Mathf.Repeat(t, 0.4f) < 0.2f ? "walk1" : "walk2";
                yield return null;
            }
            n.Pose = null;
        }

        static IEnumerator OttoScene(LevelRuntime L, Ent e)
        {
            var otto = L.NpcById("otto");
            yield return L.Letterbox(true);
            yield return L.WalkTo(26.4f);
            L.StartCoroutine(Cam.I.PanTo(new Vector2(27.5f, 6), 4.5f, 1.2f));
            yield return L.Say("Otto Brask", "Now where did you get that? That's my crest on the lid. A clock with a broken hand. I only drew it last week.", otto);
            yield return L.Say("Otto Brask", "Hand it here... Hm. Cracked clean through. I can wake it a little. Three jumps, maybe.", otto);
            L.player.PoseOverride = "hold1";
            AudioDir.I.Play("refill", 0.8f);
            UI.I.Flash(0.4f);
            L.Charges = 3;
            yield return L.Say("", "The Tether now holds 3 charges. Each Shift uses one.");
            L.player.PoseOverride = null;
            yield return L.Say("Otto Brask", "My lamps burn the same glowing stone they dig from that tunnel. Stand by one and it may wake your gadget again.", otto);
            yield return L.Say("Otto Brask", "The miners are headed for the deep vein. Mind the gas.", otto);
            yield return L.Letterbox(false);
        }

        static IEnumerator VeinScene(LevelRuntime L, Ent e)
        {
            yield return L.Letterbox(true);
            if (!L.InA) { L.ForceEra(true); yield return new WaitForSeconds(0.5f); }
            yield return L.WalkTo(143f);
            yield return Cam.I.PanTo(new Vector2(150, 8), 7f, 2f);
            yield return L.Say("", "The Brask vein. The light that ate the city, still asleep in the rock.");
            yield return L.Say("", "Wren reaches for the Tether, to Shift forward one last time and warn them.");
            L.player.PoseOverride = "hold1";
            AudioDir.I.Play("charge", 0.8f);
            yield return new WaitForSeconds(0.5f);
            AudioDir.I.Play("crack", 1f);
            AudioDir.I.Play("blocked", 0.8f);
            Cam.I.Shake(0.4f);
            UI.I.Flash(0.6f);
            L.Charges = 0;
            L.ShiftLocked = true;
            yield return UI.I.Card("", "The Tether cracks.", 1.2f);

            var otto = L.NpcById("otto2");
            otto.transform.position = new Vector3(134, 4);
            yield return Cam.I.PanTo(new Vector2(140, 6), 4.5f, 1f);
            yield return NpcWalk(otto, 141.2f, 2.4f);
            otto.Face(false);
            L.player.FacingLeft = true;
            yield return L.Say("Otto Brask", "Easy now. Let me see it.", otto);
            yield return L.Say("Otto Brask", "Broken for good. I can't mend this in one lifetime. But my boys could. And their children after them.", otto);
            L.player.PoseOverride = "hold2";
            yield return L.Say("", "Wren places the broken parts in Otto's hands.");
            L.player.PoseOverride = null;
            otto.Pose = "hold1";
            yield return L.Say("Otto Brask", "Then my family will finish it.", otto);
            L.StartCoroutine(Cam.I.PanTo(new Vector2(150, 9), 9f, 5f));
            yield return UI.I.Card("", "Three hundred years later, a scavenger will find it in a tower.", 2.5f);
            yield return UI.I.FadeTo(1, 1f);
            L.Finish();
        }

        // =====================================================================
        // Chapter 5: The Last Shift (1889, no Tether, cave-in chase)
        // =====================================================================
        static LevelPlan LastShift()
        {
            var p = new LevelPlan("The Last Shift", "Chapter 5", Era.Past, Era.Past, 165, 20);
            p.ShiftEnabled = false;
            p.Charges = 0;
            p.Underground = true;
            p.Chase = true;
            p.ChaseSpeed = 3.8f;
            p.MusicA = "cave";
            p.Start = new Vector2(3, 4);

            p.Block(0, 16, 165, 4);              // cave roof
            p.Ground(0, 24, 4);
            p.Prop("glow", 6, 7, EM.Both, 5f, new Color(0.5f, 0.95f, 1f, 0.35f));
            p.Prop("crystal", 6, 4, EM.Both, 2.6f);
            p.Prop("support", 13, 4, EM.Both, 1f);
            p.Prop("support", 17, 4, EM.Both, 1f);
            p.Cue("kick", 11, KickScene);

            p.Ground(27, 10, 4);
            p.Checkpoint(29, 4);
            p.Ground(37, 8, 6);
            p.Ground(45, 6, 8);
            p.Ground(54, 12, 6);
            p.Checkpoint(55, 6);
            p.Block(59, 7, 5, 9);                // low passage: slide!
            p.Ground(66, 6, 4);
            p.Ground(76, 10, 4);
            p.Checkpoint(77, 4);
            p.Ladder(85, 4, 8);
            p.Ground(86, 20, 12);
            p.Ground(106, 10, 7);
            p.Checkpoint(107, 7);
            p.Crumble(116, 6, 4);
            p.Ground(120, 10, 7);
            p.Checkpoint(121, 7);
            p.Ground(134, 31, 5);
            p.Exit(152, 5);
            p.Text(57, 6, "Slide! Run and press {DOWN}.", null, false, 3);
            p.Text(83, 4, "Climb!", null, false, 2);

            p.Intro = L => ChapterIntro(L, "Chapter 5", "The Last Shift", "The Tether is gone. There is no way home.");
            p.Outro = Dawn;
            return p;
        }

        static IEnumerator KickScene(LevelRuntime L, Ent e)
        {
            yield return L.Letterbox(true);
            yield return L.WalkTo(14.6f);
            L.player.FacingLeft = false;
            yield return Cam.I.PanTo(new Vector2(11, 7), 5f, 1.5f);
            yield return L.Say("", "These beams hold up the vein chamber.");
            yield return L.Say("", "If the light is never found, the Lantern is never built. And the Bloom never happens.");
            UI.I.Toast("Press {INTERACT} to kick the support.");
            while (!GInput.InteractDown && !GInput.ConfirmDown) yield return null;
            L.player.PoseOverride = "kick";
            AudioDir.I.Play("kick", 1f);
            AudioDir.I.Play("crack", 1f);
            Cam.I.Shake(0.6f);
            yield return new WaitForSeconds(0.5f);
            L.player.PoseOverride = null;
            AudioDir.I.Play("rumble", 1f);
            UI.I.Toast("The mountain is coming down. RUN!");
            AudioDir.I.PlayMusic("chase");
            yield return L.Letterbox(false);
            L.StartChase();
        }

        static IEnumerator Dawn(LevelRuntime L)
        {
            L.StopChase();
            AudioDir.I.Play("rumble", 1f);
            UI.I.Flash(0.8f);
            AudioDir.I.FadeOutMusic();
            yield return UI.I.FadeTo(1, 1.2f);
            yield return new WaitForSeconds(0.6f);
            AudioDir.I.PlayMusic("theme");
            yield return UI.I.Card("", "Wren climbs out at dawn as the mine falls shut behind.", 2.4f);
            yield return UI.I.Card("", "Somewhere far ahead, a city will grow up green.", 2.2f);
            yield return UI.I.Card("", "You create a better world without you.", 3f);
        }

        // =====================================================================
        // Epilogue: Halden 2190, saved
        // =====================================================================
        static LevelPlan Epilogue()
        {
            var p = new LevelPlan("Halden, 2190", "Epilogue", Era.Saved, Era.Saved, 90, 16);
            p.ShiftEnabled = false;
            p.MusicA = "epilogue";
            p.Start = new Vector2(2, 3);
            p.Ground(0, 90, 3);
            p.Prop("tree", 12, 3); p.Prop("tree", 30, 3, EM.Both, 1.2f); p.Prop("tree", 70, 3, EM.Both, 1.4f);
            p.Prop("tree", 76, 3);
            p.Npc("gardener", "Soldier", "Gardener", 20, 3, EM.A, false, new Color(0.7f, 0.95f, 0.7f));
            p.Npc("ivy", "Female", "Ivy Vale", 40, 3, EM.A, false);
            p.Prop("easel", 42.5f, 3);
            p.Npc("brask", "Adventurer", "Mara Brask", 57, 3, EM.A, true, new Color(0.95f, 0.9f, 1f));
            p.Prop("shopsign", 55, 6.6f);
            p.Prop("window", 52.5f, 3.6f, EM.Both, 1.4f);
            p.Prop("tether", 52.5f, 4.4f, EM.Both, 1.2f);
            p.Npc("kid", "Player", "Kid", 66, 3, EM.A, false, new Color(1f, 0.92f, 0.8f));
            p.Intro = EpilogueScene;
            p.Outro = RollCredits;
            return p;
        }

        static IEnumerator EpilogueScene(LevelRuntime L)
        {
            L.player.sr.enabled = false;
            UI.I.LetterboxTarget = 1;
            Cam.I.Hold(new Vector2(6, 6), 5.5f);
            L.StartCoroutine(Cam.I.PanTo(new Vector2(22, 6), 5.5f, 6f));
            yield return UI.I.Card("Halden, 2190", "", 2.2f);
            yield return UI.I.Card("", "There is no Lantern. There never was a Bloom.", 2.2f);
            var ivy = L.NpcById("ivy");
            ivy.Pose = "hold1";
            yield return Cam.I.PanTo(new Vector2(41, 5.5f), 4.2f, 3f);
            ivy.Pose = null;
            yield return L.Say("Ivy Vale", "Hold still, I'm almost done with the sky. Both hands today, no excuses.", ivy);
            yield return L.Say("Ivy Vale", "Funny thing. I keep painting a tower that was never built. Great-grandma Ines used to dream about it.", ivy);
            ivy.Pose = "hold2";
            yield return Cam.I.PanTo(new Vector2(55, 5.5f), 4.2f, 3f);
            var brask = L.NpcById("brask");
            yield return L.Say("", "A shop sign: BRASK & DAUGHTERS. CLOCKS SINCE 1889.");
            yield return L.Say("Mara Brask", "That one in the window? Not for sale. It's been in the family for three hundred years. Never worked once.", brask);
            yield return L.Say("Mara Brask", "Some days I swear it's warm.", brask);
            yield return Cam.I.PanTo(new Vector2(72, 7), 6f, 4f);
            yield return UI.I.Card("", "Nobody here remembers Wren.", 2f);
            yield return UI.I.Card("", "Nothing went wrong for them to remember.", 2.6f);
            L.Finish();
        }

        static IEnumerator RollCredits(LevelRuntime L)
        {
            yield return UI.I.Credits(new[]
            {
                "#TETHER",
                "",
                "A cinematic platformer",
                "COMP 4478  Project 2  Group 3",
                "",
                "#Story, design and code",
                "Group 3",
                "",
                "#Characters",
                "Platformer Characters by Kenney (kenney.nl)",
                "License: Creative Commons Zero (CC0)",
                "",
                "#Music and sound",
                "Composed and synthesized in code for this game",
                "",
                "#Thank you for playing",
                "",
                "You create a better world without you.",
            });
            yield return UI.I.FadeTo(1, 1.5f);
        }
    }
}
