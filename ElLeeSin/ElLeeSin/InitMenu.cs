﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

using SharpDX;

using Color = System.Drawing.Color;

namespace ElLeeSin
{
    using System.Drawing;

    public class InitMenu
    {
        public static Menu Menu;

        public static void Initialize()
        {
            //Base menu
            Menu = new Menu("ElLeeSin", "LeeSin", true);
            //Orbwalker and menu
            Menu.AddSubMenu(new Menu("Orbwalker", "Orbwalker"));
            Program.Orbwalker = new Orbwalking.Orbwalker(Menu.SubMenu("Orbwalker"));
            //Target selector and menu
            var ts = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(ts);
            Menu.AddSubMenu(ts);
            //Combo menu
            Menu.AddSubMenu(new Menu("Combo", "Combo"));
            Menu.SubMenu("Combo").AddItem(new MenuItem("ElLeeSin.Combo.Q", "Use Q").SetValue(true));
            Menu.SubMenu("Combo").AddItem(new MenuItem("ElLeeSin.Combo.Q2", "Use Q2").SetValue(true));
            Menu.SubMenu("Combo").AddItem(new MenuItem("ElLeeSin.Combo.W", "Wardjump in combo").SetValue(false));
            Menu.SubMenu("Combo").AddItem(new MenuItem("ElLeeSin.Combo.Seperator", "Wardjump if: "));
            Menu.SubMenu("Combo")
                .AddItem(new MenuItem("ElLeeSin.Combo.Mode.W", "> AA Range || > Q Range").SetValue(true));
            Menu.SubMenu("Combo").AddItem(new MenuItem("ElLeeSin.Combo.E", "Use E").SetValue(true));
            Menu.SubMenu("Combo").AddItem(new MenuItem("ElLeeSin.Combo.W2", "Use W").SetValue(true));
            Menu.SubMenu("Combo").AddItem(new MenuItem("ElLeeSin.Combo.R", "Use R").SetValue(true));
            Menu.SubMenu("Combo").AddItem(new MenuItem("ElLeeSin.Combo.KS.R", "KS R").SetValue(true));
            Menu.SubMenu("Combo")
                .AddItem(
                    new MenuItem("starCombo", "Star Combo").SetValue(
                        new KeyBind("T".ToCharArray()[0], KeyBindType.Press)));
            Menu.SubMenu("Combo").AddItem(new MenuItem("ElLeeSin.Combo.Seperator.1", "W->Q->R->Q2"));
            Menu.SubMenu("Combo").AddItem(new MenuItem("ElLeeSin.Combo.AAStacks", "Wait for Passive").SetValue(false));

            var harassMenu = new Menu("Harass", "Harass");
            {
                harassMenu.AddItem(new MenuItem("ElLeeSin.Harass.Q1", "Use Q1").SetValue(true));
                harassMenu.AddItem(new MenuItem("ElLeeSin.Harass.Q2", "Use Q2").SetValue(true));
                harassMenu.AddItem(new MenuItem("ElLeeSin.Harass.Wardjump", "Wardjump/Minion Jump away").SetValue(true));
                harassMenu.AddItem(new MenuItem("ElLeeSin.Harass.E1", "Use E1").SetValue(false));
            }
            Menu.AddSubMenu(harassMenu);

            var waveclearMenu = new Menu("Clear", "Clear");
            {
                waveclearMenu.SubMenu("Laneclear").AddItem(new MenuItem("sjasjsdsjs", "WaveClear"));
                waveclearMenu.SubMenu("Laneclear").AddItem(new MenuItem("ElLeeSin.Lane.Q", "Use Q").SetValue(true));
                waveclearMenu.SubMenu("Laneclear").AddItem(new MenuItem("ElLeeSin.Lane.E", "Use E").SetValue(true));

                waveclearMenu.SubMenu("Jungleclear").AddItem(new MenuItem("ElLeeSin.Jungle.Q", "Use Q").SetValue(true));
                waveclearMenu.SubMenu("Jungleclear").AddItem(new MenuItem("ElLeeSin.Jungle.W", "Use W").SetValue(true));
                waveclearMenu.SubMenu("Jungleclear").AddItem(new MenuItem("ElLeeSin.Jungle.E", "Use E").SetValue(true));
            }

            Menu.AddSubMenu(waveclearMenu);

            //InsecMenu
            var insecMenu = new Menu("Insec", "Insec").SetFontStyle(FontStyle.Bold, SharpDX.Color.Green);
            {
                insecMenu.AddItem(
                    new MenuItem("InsecEnabled", "Insec key:").SetValue(
                        new KeyBind("Y".ToCharArray()[0], KeyBindType.Press)));
                insecMenu.AddItem(new MenuItem("rnshsasdhjk", "Insec Mode:")).SetFontStyle(FontStyle.Bold, SharpDX.Color.Green);
                insecMenu.AddItem(new MenuItem("insecMode", "Left click target to Insec").SetValue(true));
                insecMenu.AddItem(new MenuItem("insecOrbwalk", "Orbwalking").SetValue(true));
                insecMenu.AddItem(new MenuItem("flashInsec", "Flash Insec when no ward").SetValue(false));
                insecMenu.AddItem(new MenuItem("waitForQBuff", "Wait For Q").SetValue(false));
                insecMenu.AddItem(new MenuItem("clickInsec", "Click Insec").SetValue(true));

                insecMenu.SubMenu("Insec Modes").AddItem(new MenuItem("ElLeeSin.Insec.Ally", "Insec to allies").SetValue(true));
                insecMenu.SubMenu("Insec Modes").AddItem(new MenuItem("ElLeeSin.Insec.Tower", "Insec to tower").SetValue(false));
                insecMenu.SubMenu("Insec Modes").AddItem(new MenuItem("ElLeeSin.Insec.Original.Pos", "Insec to original pos").SetValue(true));
                insecMenu.SubMenu("Insec Modes").AddItem(new MenuItem("insecmouse", "Insec to mouse").SetValue(false));

                insecMenu.AddItem(new MenuItem("ElLeeSin.Insec.UseInstaFlash", "Flash insec").SetValue(true));
                insecMenu.AddItem(
                    new MenuItem("ElLeeSin.Insec.Insta.Flashx", "Flash Insec key: ").SetValue(
                        new KeyBind("P".ToCharArray()[0], KeyBindType.Press)));

            }
            Menu.AddSubMenu(insecMenu);

            //Wardjump menu
            var wardjumpMenu = new Menu("Wardjump", "Wardjump");
            {
                wardjumpMenu.AddItem(
                    new MenuItem("ElLeeSin.Wardjump", "Wardjump key").SetValue(
                        new KeyBind("G".ToCharArray()[0], KeyBindType.Press)));
                wardjumpMenu.AddItem(new MenuItem("ElLeeSin.Wardjump.Mouse", "Move to mouse").SetValue(true));
                wardjumpMenu.AddItem(new MenuItem("ElLeeSin.Wardjump.Minions", "Jump to minions").SetValue(true));
                wardjumpMenu.AddItem(new MenuItem("ElLeeSin.Wardjump.Champions", "Jump to champions").SetValue(true));
            }
            Menu.AddSubMenu(wardjumpMenu);

            var drawMenu = new Menu("Drawing", "Drawing");
            {
                drawMenu.AddItem(new MenuItem("DrawEnabled", "Draw Enabled").SetValue(false));
                drawMenu.AddItem(new MenuItem("ElLeeSin.Draw.Insec.Text", "Draw insec text").SetValue(true));
                drawMenu.AddItem(new MenuItem("drawOutLineST", "Draw Outline").SetValue(true));
                drawMenu.AddItem(new MenuItem("ElLeeSin.Draw.Insec", "Draw INSEC").SetValue(true));
                drawMenu.AddItem(new MenuItem("ElLeeSin.Draw.WJDraw", "Draw WardJump").SetValue(true));
                drawMenu.AddItem(new MenuItem("ElLeeSin.Draw.Q", "Draw Q").SetValue(true));
                drawMenu.AddItem(new MenuItem("ElLeeSin.Draw.W", "Draw W").SetValue(true));
                drawMenu.AddItem(new MenuItem("ElLeeSin.Draw.E", "Draw E").SetValue(true));
                drawMenu.AddItem(new MenuItem("ElLeeSin.Draw.R", "Draw R").SetValue(true));
            }
            Menu.AddSubMenu(drawMenu);

            var miscMenu = new Menu("Misc", "Misc");
            {
                miscMenu.AddItem(new MenuItem("IGNks", "Use Ignite?").SetValue(true));
                miscMenu.AddItem(new MenuItem("qSmite", "Smite Q!").SetValue(false));
            }
            Menu.AddSubMenu(miscMenu);

            Menu.AddToMainMenu();
        }
    }
}