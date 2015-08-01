﻿namespace FuckingAwesomeLeeSin
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using LeagueSharp;
    using LeagueSharp.Common;

    using SharpDX;

    using Color = System.Drawing.Color;

    using ItemData = LeagueSharp.Common.Data.ItemData;


    internal class Program
    {
        #region Params


        private static string ChampName = "LeeSin";

        private static Orbwalking.Orbwalker Orbwalker;

        private static Obj_AI_Hero Player = ObjectManager.Player;

        public static Spell Q, W, E, R;

        private static Spellbook SBook;

        private static Vector2 JumpPos;

        public static Vector3 mouse = Game.CursorPos;

        private static SpellSlot smiteSlot;

        private static SpellSlot flashSlot;

        private static Menu Menu;

        private static bool CastQAgain;

        private static bool castWardAgain = true;

        private static bool reCheckWard = true;

        private static bool wardJumped;

        private static Obj_AI_Base minionerimo;

        private static bool delayW;

        private static Vector2 insecLinePos;

        private static float TimeOffset;

        private static Vector3 lastWardPos;

        private static float lastPlaced;

        private static int passiveStacks;

        private static float passiveTimer;

        private static bool waitforjungle;

        private static bool waitingForQ2;

        private static bool q2Done;

        private static float q2Timer;

        private static int clickCount;

        private static Vector3 insecClickPos;

        private static float resetTime;

        private static bool clicksecEnabled;

        private static float doubleClickReset;

        private static Vector3 lastClickPos;

        private static bool lastClickBool;

        private static bool textRendered;

        private static SpellSlot igniteSlot;

        private enum InsecComboStepSelect
        {
            None, Qgapclose, Wgapclose, Pressr
        };

        private static readonly string[] spells =
        {
            "BlindMonkQOne", "BlindMonkWOne", "BlindMonkEOne", "blindmonkwtwo",
            "blindmonkqtwo", "blindmonketwo", "BlindMonkRKick"
        };

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnWndProc(WndEventArgs args)
        {
            if (args.Msg != (uint)WindowsMessages.WM_LBUTTONDOWN || !ParamBool("clickInsec"))
            {
                return;
            }
            var asec =
                ObjectManager.Get<Obj_AI_Hero>()
                    .Where(a => a.IsEnemy && a.Distance(Game.CursorPos) < 200 && a.IsValid && !a.IsDead);
            if (asec.Any())
            {
                return;
            }
            if (!lastClickBool || clickCount == 0)
            {
                clickCount++;
                lastClickPos = Game.CursorPos;
                lastClickBool = true;
                doubleClickReset = Environment.TickCount + 600;
                return;
            }
            if (lastClickBool && lastClickPos.Distance(Game.CursorPos) < 200)
            {
                clickCount++;
                lastClickBool = false;
            }
        }

        private static void OrbwalkingAfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (unit.IsMe && passiveStacks > 0)
            {
                passiveStacks = passiveStacks - 1;
            }
        }

        #endregion

        private static void GameObject_OnDelete(GameObject sender, EventArgs args)
        {
            if (!(sender is Obj_GeneralParticleEmitter))
            {
                return;
            }
            if (sender.Name.Contains("blindMonk_Q_resonatingStrike") && waitingForQ2)
            {
                waitingForQ2 = false;
                q2Done = true;
                q2Timer = Environment.TickCount + 800;
            }
        }

        #region OnLoad

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (Player.ChampionName != ChampName)
            {
                return;
            }
            igniteSlot = Player.GetSpellSlot("SummonerDot");
            flashSlot = Player.GetSpellSlot("summonerflash");

            Q = new Spell(SpellSlot.Q, 1100);
            W = new Spell(SpellSlot.W, 700);
            E = new Spell(SpellSlot.E, 430);
            R = new Spell(SpellSlot.R, 375);

            Q.SetSkillshot(
                Q.Instance.SData.SpellCastTime,
                Q.Instance.SData.LineWidth,
                Q.Instance.SData.MissileSpeed,
                true,
                SkillshotType.SkillshotLine);

            //Base menu
            Menu = new Menu("FALeeSin", ChampName, true);
            //Orbwalker and menu
            Menu.AddSubMenu(new Menu("Orbwalker", "Orbwalker"));
            Orbwalker = new Orbwalking.Orbwalker(Menu.SubMenu("Orbwalker"));
            //Target selector and menu
            var ts = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(ts);
            Menu.AddSubMenu(ts);
            //Combo menu
            Menu.AddSubMenu(new Menu("Combo", "Combo"));
            Menu.SubMenu("Combo").AddItem(new MenuItem("useQ", "Use Q").SetValue(true));
            Menu.SubMenu("Combo").AddItem(new MenuItem("useQ2", "Use Q2").SetValue(true));
            Menu.SubMenu("Combo").AddItem(new MenuItem("useW", "Wardjump in combo").SetValue(false));
            Menu.SubMenu("Combo").AddItem(new MenuItem("dsjk", "Wardjump if: "));
            Menu.SubMenu("Combo").AddItem(new MenuItem("wMode", "> AA Range || > Q Range").SetValue(true));
            Menu.SubMenu("Combo").AddItem(new MenuItem("useE", "Use E").SetValue(true));
            Menu.SubMenu("Combo").AddItem(new MenuItem("useWCombo", "Use W").SetValue(true));
            Menu.SubMenu("Combo").AddItem(new MenuItem("useR", "Use R").SetValue(true));
            Menu.SubMenu("Combo").AddItem(new MenuItem("ksR", "KS R").SetValue(true));
            Menu.SubMenu("Combo")
                .AddItem(
                    new MenuItem("starCombo", "Star Combo").SetValue(
                        new KeyBind("T".ToCharArray()[0], KeyBindType.Press)));
            Menu.SubMenu("Combo").AddItem(new MenuItem("random2ejwej", "W->Q->R->Q2"));
            Menu.SubMenu("Combo").AddItem(new MenuItem("aaStacks", "Wait for Passive").SetValue(false));

            var harassMenu = new Menu("Harass", "Harass");
            harassMenu.AddItem(new MenuItem("q1H", "Use Q1").SetValue(true));
            harassMenu.AddItem(new MenuItem("q2H", "Use Q2").SetValue(true));
            harassMenu.AddItem(new MenuItem("wH", "Wardjump/Minion Jump away").SetValue(true));
            harassMenu.AddItem(new MenuItem("eH", "Use E1").SetValue(false));
            Menu.AddSubMenu(harassMenu);

            //Jung/Wave Clear
            var waveclearMenu = new Menu("Wave/Jung Clear", "wjClear");
            waveclearMenu.AddItem(new MenuItem("sjasjsdsjs", "WaveClear"));
            waveclearMenu.AddItem(new MenuItem("useQClear", "Use Q").SetValue(true));
            waveclearMenu.AddItem(new MenuItem("useEClear", "Use E").SetValue(true));
            waveclearMenu.AddItem(new MenuItem("sjasjjs", "Jungle"));
 
            waveclearMenu.AddItem(new MenuItem("Qjng", "Use Q").SetValue(true));
            waveclearMenu.AddItem(new MenuItem("Wjng", "Use W").SetValue(true));
            waveclearMenu.AddItem(new MenuItem("Ejng", "Use E").SetValue(true));
            Menu.AddSubMenu(waveclearMenu);

            //InsecMenu
            var insecMenu = new Menu("Insec", "Insec");
            insecMenu.AddItem(
                new MenuItem("InsecEnabled", "Enabled").SetValue(new KeyBind("Y".ToCharArray()[0], KeyBindType.Press)));
            insecMenu.AddItem(new MenuItem("rnshsasdhjk", "Insec Mode:"));
            insecMenu.AddItem(new MenuItem("insecMode", "Left Click [on] TS [off]").SetValue(true));
            insecMenu.AddItem(new MenuItem("insecOrbwalk", "Orbwalking").SetValue(true));
            insecMenu.AddItem(new MenuItem("flashInsec", "Flash insec").SetValue(false));
            insecMenu.AddItem(new MenuItem("waitForQBuff", "Wait For Q Buff to go").SetValue(false));
            insecMenu.AddItem(new MenuItem("22222222222222", "(Faster off more dmg on)"));
            insecMenu.AddItem(new MenuItem("clickInsec", "Click Insec").SetValue(true));
            var lM = insecMenu.AddSubMenu(new Menu("Click Insec Instructions", "clickInstruct"));
            lM.AddItem(new MenuItem("1223342334", "Firstly Click the point you want to"));
            lM.AddItem(new MenuItem("122334233", "Two Times. Then Click your target and insec"));
            insecMenu.AddItem(new MenuItem("insec2champs", "Insec to allies").SetValue(true));
            insecMenu.AddItem(new MenuItem("bonusRangeA", "Ally Bonus Range").SetValue(new Slider(0, 0, 1000)));
            insecMenu.AddItem(new MenuItem("insec2tower", "Insec to towers").SetValue(true));
            insecMenu.AddItem(new MenuItem("bonusRangeT", "Towers Bonus Range").SetValue(new Slider(0, 0, 1000)));
            insecMenu.AddItem(new MenuItem("insec2orig", "Insec to original pos").SetValue(true));
            insecMenu.AddItem(new MenuItem("22222222222", "--"));
            insecMenu.AddItem(new MenuItem("instaFlashInsec1", "Cast R Manually"));
            insecMenu.AddItem(new MenuItem("instaFlashInsec2", "And it will flash to insec pos"));
            insecMenu.AddItem(
                new MenuItem("instaFlashInsec", "Enabled").SetValue(
                    new KeyBind("P".ToCharArray()[0], KeyBindType.Toggle)));
            Menu.AddSubMenu(insecMenu);

            //Wardjump menu
            var wardjumpMenu = new Menu("Wardjump", "Wardjump");
            wardjumpMenu.AddItem(
                new MenuItem("wjump", "Wardjump key").SetValue(new KeyBind("G".ToCharArray()[0], KeyBindType.Press)));
            wardjumpMenu.AddItem(new MenuItem("m2m", "Move to mouse").SetValue(true));
            wardjumpMenu.AddItem(new MenuItem("j2m", "Jump to minions").SetValue(true));
            wardjumpMenu.AddItem(new MenuItem("j2c", "Jump to champions").SetValue(true));
            Menu.AddSubMenu(wardjumpMenu);

            var drawMenu = new Menu("Drawing", "Drawing");
            drawMenu.AddItem(new MenuItem("DrawEnabled", "Draw Enabled").SetValue(false));
            drawMenu.AddItem(new MenuItem("drawST", "Draw Smite Text").SetValue(true));
            drawMenu.AddItem(new MenuItem("drawOutLineST", "Draw Outline").SetValue(true));
            drawMenu.AddItem(new MenuItem("insecDraw", "Draw INSEC").SetValue(true));
            drawMenu.AddItem(new MenuItem("WJDraw", "Draw WardJump").SetValue(true));
            drawMenu.AddItem(new MenuItem("drawQ", "Draw Q").SetValue(true));
            drawMenu.AddItem(new MenuItem("drawW", "Draw W").SetValue(true));
            drawMenu.AddItem(new MenuItem("drawE", "Draw E").SetValue(true));
            drawMenu.AddItem(new MenuItem("drawR", "Draw R").SetValue(true));
            Menu.AddSubMenu(drawMenu);

            var miscMenu = new Menu("Misc", "Misc");
            miscMenu.AddItem(
                new MenuItem("QHC", "Q Hitchance").SetValue(
                    new StringList(new[] { "LOW", "MEDIUM", "HIGH", "VERY HIGH" }, 1)));
            miscMenu.AddItem(new MenuItem("IGNks", "Use Ignite?").SetValue(true));
            Menu.AddSubMenu(miscMenu);

            Menu.AddToMainMenu();

            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnUpdate += Game_OnGameUpdate; 
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            GameObject.OnCreate += GameObject_OnCreate;
            Orbwalking.AfterAttack += OrbwalkingAfterAttack;
            GameObject.OnDelete += GameObject_OnDelete;
            Game.OnWndProc += Game_OnWndProc;
        }

        #endregion

        #region Harass

        private static void Harass()
        {
            var target = TargetSelector.GetTarget(Q.Range + 200, TargetSelector.DamageType.Physical);
            var q = ParamBool("q1H");
            var q2 = ParamBool("q2H");
            var e = ParamBool("eH");
            var w = ParamBool("wH");

            if (q && Q.IsReady() && Q.Instance.Name == "BlindMonkQOne" && target.IsValidTarget(Q.Range))
            {
                CastQ1(target);
            }
            if (q2 && Q.IsReady()
                && (target.HasBuff("BlindMonkQOne") || target.HasBuff("blindmonkqonechaos")))
            {
                if (CastQAgain || !target.IsValidTarget(Orbwalking.GetRealAutoAttackRange(Player)))
                {
                    Q.Cast();
                }
            }
            if (e && E.IsReady() && target.IsValidTarget(E.Range) && E.Instance.Name == "BlindMonkEOne" )
            {
                E.Cast();
            }

            if (w && Player.Distance(target) < 50
                && !(target.HasBuff("BlindMonkQOne") && !target.HasBuff("blindmonkqonechaos"))
                && (E.Instance.Name == "blindmonketwo" || !E.IsReady() && e)
                && (Q.Instance.Name == "blindmonkqtwo" || !Q.IsReady() && q))
            {
                var min =
                    ObjectManager.Get<Obj_AI_Minion>()
                        .Where(a => a.IsAlly && a.Distance(Player) <= W.Range)
                        .OrderByDescending(a => a.Distance(target))
                        .FirstOrDefault();

                W.CastOnUnit(min);
            }
        }

        #endregion

        #region Insec

        private static bool isNullInsecPos = true;

        private static Vector3 insecPos;

        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe)
            {
                return;
            }
            if (spells.Contains(args.SData.Name))
            {
                passiveStacks = 2;
                passiveTimer = Environment.TickCount + 3000;
            }
            if (args.SData.Name == "BlindMonkQOne")
            {
                CastQAgain = false;
                Utility.DelayAction.Add(2900, () => { CastQAgain = true; });
            }

            if (Menu.Item("instaFlashInsec").GetValue<KeyBind>().Active && args.SData.Name == "BlindMonkRKick" && Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.Combo)
            {
                Player.Spellbook.CastSpell(flashSlot, GetInsecPos((Obj_AI_Hero)(args.Target)));
            }

            if (args.SData.Name == "summonerflash" && InsecComboStep != InsecComboStepSelect.None)
            {
                Obj_AI_Hero target = ParamBool("insecMode")
                                         ? TargetSelector.GetSelectedTarget()
                                         : TargetSelector.GetTarget(Q.Range + 200, TargetSelector.DamageType.Physical);
                InsecComboStep = InsecComboStepSelect.Pressr;
                Utility.DelayAction.Add(80, () => R.CastOnUnit(target, true));
            }
            if (args.SData.Name == "blindmonkqtwo")
            {
                waitingForQ2 = true;
                Utility.DelayAction.Add(3000, () => { waitingForQ2 = false; });
            }
            if (args.SData.Name == "BlindMonkRKick")
            {
                InsecComboStep = InsecComboStepSelect.None;
            }
        }

        private static Vector3 GetInsecPos(Obj_AI_Hero target)
        {
            if (clicksecEnabled && ParamBool("clickInsec"))
            {
                insecLinePos = Drawing.WorldToScreen(insecClickPos);
                return V2E(insecClickPos, target.Position, target.Distance(insecClickPos) + 230).To3D();
            }
            if (isNullInsecPos)
            {
                isNullInsecPos = false;
                insecPos = Player.Position;
            }
            var turrets = (from tower in ObjectManager.Get<Obj_Turret>()
                           where
                               tower.IsAlly && !tower.IsDead
                               && target.Distance(tower.Position)
                               < 1500 + Menu.Item("bonusRangeT").GetValue<Slider>().Value && tower.Health > 0
                           select tower).ToList();
            if (GetAllyHeroes(target, 2000 + Menu.Item("bonusRangeA").GetValue<Slider>().Value).Count > 0
                && ParamBool("insec2champs"))
            {
                Vector3 insecPosition =
                    InterceptionPoint(
                        GetAllyInsec(GetAllyHeroes(target, 2000 + Menu.Item("bonusRangeA").GetValue<Slider>().Value)));
                insecLinePos = Drawing.WorldToScreen(insecPosition);
                return V2E(insecPosition, target.Position, target.Distance(insecPosition) + 230).To3D();
            }
            if (turrets.Any() && ParamBool("insec2tower"))
            {
                insecLinePos = Drawing.WorldToScreen(turrets[0].Position);
                return V2E(turrets[0].Position, target.Position, target.Distance(turrets[0].Position) + 230).To3D();
            }
            if (ParamBool("insec2orig"))
            {
                insecLinePos = Drawing.WorldToScreen(insecPos);
                return V2E(insecPos, target.Position, target.Distance(insecPos) + 230).To3D();
            }
            return new Vector3();
        }


        private static InsecComboStepSelect InsecComboStep;

        private static void InsecCombo(Obj_AI_Hero target)
        {
            if (target != null && target.IsVisible)
            {
                if (Player.Distance(GetInsecPos(target)) < 200)
                {
                    InsecComboStep = InsecComboStepSelect.Pressr;
                }
                else if (InsecComboStep == InsecComboStepSelect.None
                         && GetInsecPos(target).Distance(Player.Position) < 600)
                {
                    InsecComboStep = InsecComboStepSelect.Wgapclose;
                }
                else if (InsecComboStep == InsecComboStepSelect.None && target.Distance(Player) < Q.Range)
                {
                    InsecComboStep = InsecComboStepSelect.Qgapclose;
                }

                switch (InsecComboStep)
                {
                    case InsecComboStepSelect.Qgapclose:
                        if (!(target.HasBuff("BlindMonkQOne") || target.HasBuff("blindmonkqonechaos"))
                            && Q.Instance.Name == "BlindMonkQOne")
                        {
                            CastQ1(target);
                        }
                        else if ((target.HasBuff("BlindMonkQOne") || target.HasBuff("blindmonkqonechaos")))
                        {
                            Q.Cast();
                            InsecComboStep = InsecComboStepSelect.Wgapclose;
                        }
                        else
                        {
                            if (Q.Instance.Name == "blindmonkqtwo" && ReturnQBuff().Distance(target) <= 600)
                            {
                                Q.Cast();
                            }
                        }
                        break;
                    case InsecComboStepSelect.Wgapclose:
                        if (FindBestWardItem() != null && W.IsReady() && W.Instance.Name == "BlindMonkWOne"
                            && (ParamBool("waitForQBuff")
                                && (Q.Instance.Name == "BlindMonkQOne"
                                    || (!Q.IsReady() || Q.Instance.Name == "blindmonkqtwo") && q2Done))
                            || !ParamBool("waitForQBuff"))
                        {
                            WardJump(GetInsecPos(target), false, false, true);
                            wardJumped = true;
                        }
                        else if (Player.Spellbook.CanUseSpell(flashSlot) == SpellState.Ready && ParamBool("flashInsec")
                                 && !wardJumped && Player.Distance(insecPos) < 400
                                 || Player.Spellbook.CanUseSpell(flashSlot) == SpellState.Ready
                                 && ParamBool("flashInsec") && !wardJumped && Player.Distance(insecPos) < 400
                                 && FindBestWardItem() == null)
                        {
                            Player.Spellbook.CastSpell(flashSlot, GetInsecPos(target));
                            Utility.DelayAction.Add(50, () => R.CastOnUnit(target, true));
                        }
                        break;
                    case InsecComboStepSelect.Pressr:
                        R.CastOnUnit(target, true);
                        break;
                }
            }
        }

        private static Vector3 InterceptionPoint(List<Obj_AI_Hero> heroes)
        {
            Vector3 result = new Vector3();
            foreach (Obj_AI_Hero hero in heroes)
            {
                result += hero.Position;
            }
            result.X /= heroes.Count;
            result.Y /= heroes.Count;
            return result;
        }

        private static List<Obj_AI_Hero> GetAllyInsec(List<Obj_AI_Hero> heroes)
        {
            int alliesAround = 0;
            Obj_AI_Hero tempObject = new Obj_AI_Hero();
            foreach (Obj_AI_Hero hero in heroes)
            {
                int localTemp = GetAllyHeroes(hero, 500 + Menu.Item("bonusRangeA").GetValue<Slider>().Value).Count;
                if (localTemp > alliesAround)
                {
                    tempObject = hero;
                    alliesAround = localTemp;
                }
            }
            return GetAllyHeroes(tempObject, 500 + Menu.Item("bonusRangeA").GetValue<Slider>().Value);
        }

        private static List<Obj_AI_Hero> GetAllyHeroes(Obj_AI_Hero position, int range)
        {
            List<Obj_AI_Hero> temp = new List<Obj_AI_Hero>();

            foreach (Obj_AI_Hero hero in ObjectManager.Get<Obj_AI_Hero>())
            {
                if (hero.IsAlly && !hero.IsMe && hero.Distance(position) < range)
                {
                    temp.Add(hero);
                }
            }
            return temp;
        }

        private static Vector2 V2E(Vector3 from, Vector3 direction, float distance)
        {
            return from.To2D() + distance * Vector3.Normalize(direction - from).To2D();
        }

        #endregion

        #region Tick Tasks

        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (doubleClickReset <= Environment.TickCount && clickCount != 0)
            {
                doubleClickReset = float.MaxValue;
                clickCount = 0;
            }

            if (clickCount >= 2 && ParamBool("clickInsec"))
            {
                resetTime = Environment.TickCount + 3000;
                clicksecEnabled = true;
                insecClickPos = Game.CursorPos;
                clickCount = 0;
            }

            if (passiveTimer <= Environment.TickCount)
            {
                passiveStacks = 0;
            }

            if (resetTime <= Environment.TickCount && !Menu.Item("InsecEnabled").GetValue<KeyBind>().Active
                && clicksecEnabled)
            {
                clicksecEnabled = false;
            }

            if (q2Timer <= Environment.TickCount)
            {
                q2Done = false;
            }

            if (Player.IsDead)
            {
                return;
            }

            /*if (Menu.Item("jungActive").GetValue<KeyBind>().Active)
            {
                
            }*/

            if ((ParamBool("insecMode")
                     ? TargetSelector.GetSelectedTarget()
                     : TargetSelector.GetTarget(Q.Range + 200, TargetSelector.DamageType.Physical)) == null)
            {
                InsecComboStep = InsecComboStepSelect.None;
            }

            if (Menu.Item("starCombo").GetValue<KeyBind>().Active)
            {
                WardCombo();
            }

            if (ParamBool("IGNks"))
            {
                Obj_AI_Hero NewTarget = TargetSelector.GetTarget(600, TargetSelector.DamageType.True);

                if (NewTarget != null && igniteSlot != SpellSlot.Unknown
                    && Player.Spellbook.CanUseSpell(igniteSlot) == SpellState.Ready
                    && ObjectManager.Player.GetSummonerSpellDamage(NewTarget, Damage.SummonerSpell.Ignite)
                    > NewTarget.Health)
                {
                    Player.Spellbook.CastSpell(igniteSlot, NewTarget);
                }
            }

            if (Menu.Item("InsecEnabled").GetValue<KeyBind>().Active)
            {
                if (ParamBool("insecOrbwalk"))
                {
                    Orbwalk(Game.CursorPos);
                }
                Obj_AI_Hero newTarget = ParamBool("insecMode")
                                            ? TargetSelector.GetSelectedTarget()
                                            : TargetSelector.GetTarget(
                                                Q.Range + 200,
                                                TargetSelector.DamageType.Physical);

                if (newTarget != null)
                {
                    InsecCombo(newTarget);
                }
            }
            else
            {
                isNullInsecPos = true;
                wardJumped = false;
            }

            if (Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.Combo)
            {
                InsecComboStep = InsecComboStepSelect.None;
            }

            switch (Orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    StarCombo();
                    break;
                case Orbwalking.OrbwalkingMode.LaneClear:
                    AllClear();
                    JungleClear();
                    break;
                case Orbwalking.OrbwalkingMode.Mixed:
                    Harass();
                    break;
            }

            if (Menu.Item("wjump").GetValue<KeyBind>().Active)
            {
                WardjumpToMouse();
            }
        }

        #endregion

        #region Draw

        private static void Drawing_OnDraw(EventArgs args)
        {
            Obj_AI_Hero newTarget = ParamBool("insecMode")
                                        ? TargetSelector.GetSelectedTarget()
                                        : TargetSelector.GetTarget(Q.Range + 200, TargetSelector.DamageType.Physical);
            if (clicksecEnabled)
            {
                Render.Circle.DrawCircle(insecClickPos, 100, Color.White);
            }
            if (Menu.Item("instaFlashInsec").GetValue<KeyBind>().Active)
            {
                Drawing.DrawText(960, 340, Color.Red, "FLASH INSEC ENABLED");
            }
            if (newTarget != null && newTarget.IsVisible && Player.Distance(newTarget) < 3000 && ParamBool("insecDraw"))
            {
                Vector2 targetPos = Drawing.WorldToScreen(newTarget.Position);
                Drawing.DrawLine(insecLinePos.X, insecLinePos.Y, targetPos.X, targetPos.Y, 3, Color.White);
                Render.Circle.DrawCircle(GetInsecPos(newTarget), 100, Color.White);
            }
            if (!ParamBool("DrawEnabled"))
            {
                return;
            }
            foreach (var t in ObjectManager.Get<Obj_AI_Hero>())
            {
                if (t.HasBuff("BlindMonkQOne") || t.HasBuff("blindmonkqonechaos"))
                {
                    Drawing.DrawCircle(t.Position, 200, Color.Red);
                }
            }

            if (Menu.Item("wjump").GetValue<KeyBind>().Active && ParamBool("WJDraw"))
            {
                Render.Circle.DrawCircle(JumpPos.To3D(), 20, Color.Red);
                Render.Circle.DrawCircle(Player.Position, 600, Color.Red);
            }
            if (ParamBool("drawQ"))
            {
                Render.Circle.DrawCircle(Player.Position, Q.Range - 80, Q.IsReady() ? Color.LightSkyBlue : Color.Tomato);
            }
            if (ParamBool("drawW"))
            {
                Render.Circle.DrawCircle(Player.Position, W.Range - 80, W.IsReady() ? Color.LightSkyBlue : Color.Tomato);
            }
            if (ParamBool("drawE"))
            {
                Render.Circle.DrawCircle(Player.Position, E.Range - 80, E.IsReady() ? Color.LightSkyBlue : Color.Tomato);
            }
            if (ParamBool("drawR"))
            {
                Render.Circle.DrawCircle(Player.Position, R.Range - 80, R.IsReady() ? Color.LightSkyBlue : Color.Tomato);
            }
        }

        #endregion

        #region WaveClear

        private static void JungleClear()
        {
            var minion =
                MinionManager.GetMinions(
                    Player.ServerPosition,
                    Q.Range,
                    MinionTypes.All,
                    MinionTeam.Neutral,
                    MinionOrderTypes.MaxHealth).First();

            if (minion == null)
            {
                return;
            }

            var passiveIsActive = passiveStacks > 0;
            UseClearItems(minion);

            if (Q.IsReady() && ParamBool("Qjng"))
            {
                Q.Cast(minion.Position);
                Waiter();
                
                if (Q.Instance.Name != "BlindMonkQOne")
                {
                    Utility.DelayAction.Add(300, () => Q.Cast());
                    return;
                }
            }

            if (passiveIsActive || waitforjungle)
            {
                return;
            }

            if (ParamBool("Qjng")
                && Q2Damage(
                    minion,
                    Q.Instance.Name == "BlindMonkQOne" ? minion.Health - Q.GetDamage(minion) : minion.Health,
                    true) > minion.Health && Q.IsReady())
            {
                if (Q.Instance.Name == "BlindMonkQOne")
                {
                    Q.Cast(minion.Position);
                    Waiter();
                    return;
                }
                Q.Cast();
                Waiter();
                return;
            }


            if (E.IsReady() && minion.IsValidTarget(E.Range) && ParamBool("Ejng"))
            {
                E.Cast();

                if (minion.IsValidTarget(0x190))
                {
                    CastHydra();
                }
            }

            if (InAutoAttackRange(minion))
            {
                Player.IssueOrder(GameObjectOrder.AttackUnit, minion);
            }

            if (E.IsReady() && E.Instance.Name != "BlindMonkEOne"
                && !minion.IsValidTarget(Orbwalking.GetRealAutoAttackRange(Player)) && ParamBool("Ejng"))
            {
                Utility.DelayAction.Add(200, () => E.Cast());
            }

            if (ParamBool("Wjng") && W.IsReady()
                && minion.IsValidTarget(Orbwalking.GetRealAutoAttackRange(Player) + 200))
            {
                if (W.Instance.Name == "BlindMonkWOne")
                {
                    W.Cast();
                    Waiter();
                    return;
                }

                if (W.Instance.Name != "BlindMonkWOne")
                {
                    Utility.DelayAction.Add(300, () => W.Cast());
                }
                return;
            }
            if (ParamBool("Qjng") && Q.IsReady() && minion.IsValidTarget(Q.Range))
            {
                if (Q.Instance.Name == "BlindMonkQOne")
                {
                    Q.Cast(minion);
                    Waiter();
                    return;
                }
                if ((minion.HasBuff("BlindMonkQOne") || minion.HasBuff("blindmonkqonechaos")))
                {
                    Q.Cast();
                    Waiter();
                    return;
                }
            }
        }

        private static void Waiter()
        {
            waitforjungle = true;
            Utility.DelayAction.Add(300, () => waitforjungle = false);
        }

        private static void AllClear()
        {
            var minion = MinionManager.GetMinions(Player.ServerPosition, Q.Range).FirstOrDefault();
            UseClearItems(minion);
            if (minion == null || minion.Name.ToLower().Contains("ward"))
            {
                return;
            }
            if (Menu.Item("useQClear").GetValue<bool>() && Q.IsReady())
            {
                if (Q.Instance.Name == "BlindMonkQOne")
                {
                    Q.Cast(minion, true);
                }
                else if ((minion.HasBuff("BlindMonkQOne") || minion.HasBuff("blindmonkqonechaos"))
                         && (Q.IsKillable(minion, 1)) || Player.Distance(minion) > 500)
                {
                    Q.Cast();
                }
            }

            if (Menu.Item("useEClear").GetValue<bool>() && E.IsReady())
            {
                if (E.Instance.Name == "BlindMonkEOne" && minion.IsValidTarget(E.Range) && !delayW)
                {
                    E.Cast();
                    delayW = true;
                    Utility.DelayAction.Add(300, () => delayW = false);
                }
                else if (minion.HasBuff("BlindMonkEOne") && (Player.Distance(minion) > 450))
                {
                    E.Cast();
                }
            }
        }

        #endregion

        #region Wardjump

        private static void WardjumpToMouse()
        {
            WardJump(Game.CursorPos, ParamBool("m2m"), false, false, ParamBool("j2m"), ParamBool("j2c"));
        }

        private static void WardJump(
            Vector3 pos,
            bool m2m = true,
            bool maxRange = false,
            bool reqinMaxRange = false,
            bool minions = true,
            bool champions = true)
        {
            var basePos = Player.Position.To2D();
            var newPos = (pos.To2D() - Player.Position.To2D());

            if (JumpPos == new Vector2())
            {
                if (reqinMaxRange)
                {
                    JumpPos = pos.To2D();
                }
                else if (maxRange || Player.Distance(pos) > 590)
                {
                    JumpPos = basePos + (newPos.Normalized() * (590));
                }
                else
                {
                    JumpPos = basePos + (newPos.Normalized() * (Player.Distance(pos)));
                }
            }
            if (JumpPos != new Vector2() && reCheckWard)
            {
                reCheckWard = false;
                Utility.DelayAction.Add(
                    20,
                    () =>
                        {
                            if (JumpPos != new Vector2())
                            {
                                JumpPos = new Vector2();
                                reCheckWard = true;
                            }
                        });
            }
            if (m2m)
            {
                Orbwalk(pos);
            }
            if (!W.IsReady() || W.Instance.Name == "blindmonkwtwo" || reqinMaxRange && Player.Distance(pos) > W.Range)
            {
                return;
            }
            if (minions || champions)
            {
                if (champions)
                {
                    var champs = (from champ in ObjectManager.Get<Obj_AI_Hero>()
                                  where
                                      champ.IsAlly && champ.Distance(Player) < W.Range && champ.Distance(pos) < 200
                                      && !champ.IsMe
                                  select champ).ToList();
                    if (champs.Count > 0)
                    {
                        W.CastOnUnit(champs[0], true);
                        return;
                    }
                }
                if (minions)
                {
                    var minion2 = (from minion in ObjectManager.Get<Obj_AI_Minion>()
                                   where
                                       minion.IsAlly && minion.Distance(Player) < W.Range && minion.Distance(pos) < 200
                                       && !minion.Name.ToLower().Contains("ward")
                                   select minion).ToList();
                    if (minion2.Count > 0)
                    {
                        W.CastOnUnit(minion2[0], true);
                        return;
                    }
                }
            }
            var isWard = false;
            foreach (var ward in ObjectManager.Get<Obj_AI_Minion>())
            {
                if (ward.IsAlly && ward.Name.ToLower().Contains("ward") && ward.Distance(JumpPos) < 200)
                {
                    isWard = true;
                    W.CastOnUnit(ward, true);
                }
            }
            if (!isWard && castWardAgain)
            {
                var ward = FindBestWardItem();
                if (ward == null)
                {
                    return;
                }
                Player.Spellbook.CastSpell(ward.SpellSlot, JumpPos.To3D());
                castWardAgain = false;
                lastWardPos = JumpPos.To3D();
                lastPlaced = Environment.TickCount;
                Utility.DelayAction.Add(500, () => castWardAgain = true);
            }
        }

        private static InventorySlot FindBestWardItem()
        {
            InventorySlot slot = Items.GetWardSlot();
            if (slot == default(InventorySlot))
            {
                return null;
            }

            SpellDataInst sdi = GetItemSpell(slot);

            if (sdi != default(SpellDataInst) && sdi.State == SpellState.Ready)
            {
                return slot;
            }
            return slot;
        }

        private static void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
            if (Environment.TickCount < lastPlaced + 300)
            {
                var ward = (Obj_AI_Minion)sender;
                if (ward.Name.ToLower().Contains("ward") && ward.Distance(lastWardPos) < 500 && E.IsReady())
                {
                    W.Cast(ward);
                }
            }
        }

        #endregion

        #region Combo

        private static void WardCombo()
        {
            var target = TargetSelector.GetTarget(1500, TargetSelector.DamageType.Physical);

            Orbwalking.Orbwalk(
                target ?? null,
                Game.CursorPos,
                Menu.Item("ExtraWindup").GetValue<Slider>().Value,
                Menu.Item("HoldPosRadius").GetValue<Slider>().Value);

            if (target == null)
            {
                return;
            }

            UseItems(target);

            if ((target.HasBuff("BlindMonkQOne") || target.HasBuff("blindmonkqonechaos")))
            {
                if (CastQAgain || target.HasBuffOfType(BuffType.Knockup) && !Player.IsValidTarget(300) && !R.IsReady()
                    || !target.IsValidTarget(Orbwalking.GetRealAutoAttackRange(Player)) && !R.IsReady())
                {
                    Q.Cast();
                }
            }
            if (target.Distance(Player) > R.Range && target.Distance(Player) < R.Range + 580
                && (target.HasBuff("BlindMonkQOne") || target.HasBuff("blindmonkqonechaos")))
            {
                WardJump(target.Position, false);
            }
            if (E.IsReady() && E.Instance.Name == "BlindMonkEOne" && target.IsValidTarget(E.Range))
            {
                E.Cast();
            }

            if (E.IsReady() && E.Instance.Name != "BlindMonkEOne"
                && !target.IsValidTarget(Orbwalking.GetRealAutoAttackRange(Player)))
            {
                E.Cast();
            }

            if (Q.IsReady() && Q.Instance.Name == "BlindMonkQOne")
            {
                CastQ1(target);
            }

            if (R.IsReady() && Q.IsReady()
                && ((target.HasBuff("BlindMonkQOne") || target.HasBuff("blindmonkqonechaos"))))
            {
                R.CastOnUnit(target);
            }
        }

        private static void StarCombo()
        {
            var target = TargetSelector.GetTarget(1300, TargetSelector.DamageType.Physical);
            if (target == null)
            {
                return;
            }
            if ((target.HasBuff("BlindMonkQOne") || target.HasBuff("blindmonkqonechaos"))
                && ParamBool("useQ2"))
            {
                if (CastQAgain || target.HasBuffOfType(BuffType.Knockup) && !Player.IsValidTarget(300) && !R.IsReady()
                    || !target.IsValidTarget(Orbwalking.GetRealAutoAttackRange(Player))
                    || Q.GetDamage(target, 1) > target.Health
                    || ReturnQBuff().Distance(target) < Player.Distance(target)
                    && !target.IsValidTarget(Orbwalking.GetRealAutoAttackRange(Player)))
                {
                    Q.Cast();
                }
            }

            //UseItems(target);

            if (R.GetDamage(target) >= target.Health && ParamBool("ksR") && target.IsValidTarget())
            {
                R.Cast(target);
            }

            if (ParamBool("aaStacks") && passiveStacks > 0
                && target.IsValidTarget(Orbwalking.GetRealAutoAttackRange(Player) + 100))
            {
                return;
            }

            if (ParamBool("useW"))
            {
                if (ParamBool("wMode") && target.Distance(Player) > Orbwalking.GetRealAutoAttackRange(Player))
                {
                    WardJump(target.Position, false, true);
                }
                if (!ParamBool("wMode") && target.Distance(Player) > Q.Range)
                {
                    WardJump(target.Position, false, true);
                }
            }

            if (E.IsReady() && E.Instance.Name == "BlindMonkEOne" && InAutoAttackRange(target) && ParamBool("useE"))
            {
                E.Cast();

                if (target.IsValidTarget(0x190))
                {
                    CastHydra();
                }
            }

            if (W.IsReady() && W.Instance.Name == "BlindMonkWOne" && InAutoAttackRange(target) && ParamBool("useWCombo"))
            {
                W.Cast();
            }

            if (InAutoAttackRange(target))
            {
                Player.IssueOrder(GameObjectOrder.AttackUnit, target);
            }

            if (W.IsReady() && W.Instance.Name != "BlindMonkWOne" && InAutoAttackRange(target) && ParamBool("useWCombo"))
            {
                Utility.DelayAction.Add(400, () => W.Cast());
            }

            if (E.IsReady() && E.Instance.Name != "BlindMonkEOne"
                && !target.IsValidTarget(Orbwalking.GetRealAutoAttackRange(Player)) && ParamBool("useE"))
            {
                Utility.DelayAction.Add(200, () => E.Cast());
            }

            if (Q.IsReady() && Q.Instance.Name == "BlindMonkQOne" && ParamBool("useQ"))
            {
                CastQ1(target);
            }

            if (R.IsReady() && Q.IsReady() && ParamBool("useR") && R.GetDamage(target) >= target.Health)
            {
                R.CastOnUnit(target);
            }
        }

        private static void CastHydra()
        {
            if (Player.IsWindingUp) return;

            if (!ItemData.Tiamat_Melee_Only.GetItem().IsReady() &&
                !ItemData.Ravenous_Hydra_Melee_Only.GetItem().IsReady())
            {
                return;
            }

            ItemData.Tiamat_Melee_Only.GetItem().Cast();
            ItemData.Ravenous_Hydra_Melee_Only.GetItem().Cast();
        }


        private static void CastQ1(Obj_AI_Hero target)
        {
            var qpred = Q.GetPrediction(target);
            if ((qpred.CollisionObjects.Where(a => a.IsValidTarget() && a.IsMinion).ToList().Count) == 1
                && qpred.CollisionObjects[0].IsValidTarget(780))
            {
               /* Player.Spellbook.CastSpell(smiteSlot, qpred.CollisionObjects[0]);
                Utility.DelayAction.Add(Game.Ping / 2, () => Q.Cast(qpred.CastPosition));*/

                if (qpred.Hitchance >= HitChance.VeryHigh)
                    Q.Cast(target);
            }
            else if (qpred.CollisionObjects.Count == 0)
            {
                /*var minChance = GetHitChance(Menu.Item("QHC").GetValue<StringList>());
                Q.CastIfHitchanceEquals(target, minChance, true);*/
                if (qpred.Hitchance >= HitChance.VeryHigh)
                    Q.Cast(target);
            }
        }

        #endregion

        #region Utility

        private static float Q2Damage(Obj_AI_Base target, float subHP = 0, bool monster = false)
        {
            var damage = (50 + (Q.Level * 30)) + (0.09 * Player.FlatPhysicalDamageMod)
                         + ((target.MaxHealth - (target.Health - subHP)) * 0.08);
            if (monster && damage > 400)
            {
                return (float)Player.CalcDamage(target, Damage.DamageType.Physical, 400);
            }
            return (float)Player.CalcDamage(target, Damage.DamageType.Physical, damage);
        }

        private static void Orbwalk(Vector3 pos, Obj_AI_Hero target = null)
        {
            Player.IssueOrder(GameObjectOrder.MoveTo, pos);
        }

        private static SpellDataInst GetItemSpell(InventorySlot invSlot)
        {
            return Player.Spellbook.Spells.FirstOrDefault(spell => (int)spell.Slot == invSlot.Slot + 4);
        }

        private static Obj_AI_Base ReturnQBuff()
        {
            foreach (var unit in ObjectManager.Get<Obj_AI_Base>().Where(a => a.IsValidTarget(1300)))
            {
                if (unit.HasBuff("BlindMonkQOne") || unit.HasBuff("blindmonkqonechaos"))
                {
                    return unit;
                }
            }

            return null;
        }

        private static void UseItems(Obj_AI_Hero enemy)
        {
            if (Items.CanUseItem(3142) && Player.Distance(enemy) <= 600)
            {
                Items.UseItem(3142);
            }
            if (Items.CanUseItem(3144) && Player.Distance(enemy) <= 450)
            {
                Items.UseItem(3144, enemy);
            }
            if (Items.CanUseItem(3153) && Player.Distance(enemy) <= 450)
            {
                Items.UseItem(3153, enemy);
            }
            if (Items.CanUseItem(3077) && Utility.CountEnemiesInRange(350) >= 1)
            {
                Items.UseItem(3077);
            }
            if (Items.CanUseItem(3074) && Utility.CountEnemiesInRange(350) >= 1)
            {
                Items.UseItem(3074);
            }
            if (Items.CanUseItem(3143) && Utility.CountEnemiesInRange(450) >= 1)
            {
                Items.UseItem(3143);
            }
        }

        private static void UseClearItems(Obj_AI_Base enemy)
        {
            if (Items.CanUseItem(3077) && Player.Distance(enemy) < 350)
            {
                Items.UseItem(3077);
            }
            if (Items.CanUseItem(3074) && Player.Distance(enemy) < 350)
            {
                Items.UseItem(3074);
            }
        }

        private static bool ParamBool(String paramName)
        {
            return Menu.Item(paramName).GetValue<bool>();
        }

        private static float GetAutoAttackRange(Obj_AI_Base source = null, Obj_AI_Base target = null)
        {
            if (source == null)
                source = Player;

            var ret = source.AttackRange + Player.BoundingRadius;
            if (target != null)

                ret += target.BoundingRadius;

            return ret;
        }

        private static bool InAutoAttackRange(Obj_AI_Base target)
        {
            if (target == null)
                return false;

            var myRange = GetAutoAttackRange(Player, target);
            return Vector2.DistanceSquared(target.ServerPosition.To2D(), Player.ServerPosition.To2D()) <= myRange * myRange;
        }

        public static HitChance GetHitChance(StringList stringList)
        {
            switch (stringList.SelectedIndex)
            {
                case 0:
                    return HitChance.Low;
                case 1:
                    return HitChance.Medium;
                case 2:
                    return HitChance.High;
                case 3:
                    return HitChance.VeryHigh;
                default:
                    return HitChance.High;
            }
        }
        #endregion
    }
}