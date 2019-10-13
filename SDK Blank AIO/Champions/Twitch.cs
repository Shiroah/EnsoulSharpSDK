using System;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.SDK;
using Color = System.Drawing.Color;
using FontStyle = System.Drawing.FontStyle;
using SharpDX;
using SColor = SharpDX.Color;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.MenuUI.Values;

namespace BlankAIO.Champions
{
    class Twitch
    {
        public static Spell Q, W, E, R;
        public static AIHeroClient Me;
        public static bool PlayerIsKillTarget;
        public static float LastFocusTime;

        private static AIHeroClient Player;
        private static Menu MainMenu;



        public static SpellSlot IgniteSlot;

        public static void Load()
        {
            GameEvent.OnGameLoad += OnLoad;
            MainMenu = new Menu("BlankAIO", "BlankAIO", true);
        }

        private static void OnLoad()
        {
            Player = ObjectManager.Player;

            Q = new Spell(SpellSlot.Q, 0);
            W = new Spell(SpellSlot.W, 950);
            E = new Spell(SpellSlot.E, 1200);
            R = new Spell(SpellSlot.R, 975);

            W.SetSkillshot(0.25f, 100f, 1410f, false, false, EnsoulSharp.SDK.Prediction.SkillshotType.Circle);

            Chat.Print("BlankAIO.Twitch Loaded! This version is using the SDK orbwalker, please select 'SDK' with Orbwalker Selector.");

            #region Combo Menu

            var comboMenu = new Menu("Combo", "Combo Settings");

            comboMenu.Add(new MenuBool("comboQ", "Use Q", true));
            comboMenu.Add(new MenuSlider("comboQn", "Q Numbers of Enemy Champs (1-5)", 3, 1, 5));
            comboMenu.Add(new MenuBool("comboW", "Use W", true));
            comboMenu.Add(new MenuBool("comboWaa", "^ only after autoattack", true));
            comboMenu.Add(new MenuBool("comboE", "Use E", true));
            comboMenu.Add(new MenuBool("comboEonlykill", "Use E only if target is killable", true));
            comboMenu.Add(new MenuSlider("comboEstacksoverride", "^ Override this if x target have max stacks of poison", 4, 1, 5));
            comboMenu.Add(new MenuBool("comboR", "Use R", true));
            comboMenu.Add(new MenuSlider("comboRn", "^ Only if x number of enemy champions", 3, 1, 5));

            MainMenu.Add(comboMenu);

            #endregion

            #region LaneClear Menu

            var laneclearMenu = new Menu("LaneClear", "Lane Clear Settings");

            laneclearMenu.Add(new MenuBool("wminions", "Use W", false));
            laneclearMenu.Add(new MenuSlider("wminionshit", "^ Only if x minions hit", 3, 1, 6));
            laneclearMenu.Add(new MenuBool("waaresetminions", "^ Only after aa", true));
            laneclearMenu.Add(new MenuBool("laneclearE", "Use E", true));
            laneclearMenu.Add(new MenuSlider("eminions", "^ Only if x minions hit", 3, 1, 6));

            MainMenu.Add(laneclearMenu);

            #endregion

            #region KillSteal Menu

            var killstealMenu = new Menu("KillSteal", "KillSteal Settings");

            killstealMenu.Add(new MenuBool("kse", "Automatically KS Kills with E"));
            Console.WriteLine("Debug: KillSteal Menu Loaded");

            MainMenu.Add(killstealMenu);

            #endregion

            #region Misc

            var miscMenu = new Menu("Misc", "Misc Settings");

            miscMenu.Add(new MenuBool("emobs", "Try to Auto-KS Epic Monsters (Baron/Dragons) with E", false));
            miscMenu.Add(new MenuBool("stealthrecall", "Stealth Recall", true));

            MainMenu.Add(miscMenu);

            #endregion

            MainMenu.Attach();


            Game.OnTick += Game_OnUpdate;
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            if (Player != null)
            {
                try
                {
                    switch (Orbwalker.ActiveMode)
                    {
                        case OrbwalkerMode.Combo:
                            Combo();
                            break;
                        case OrbwalkerMode.LaneClear:
                            LaneClear();
                            JungleClear();
                            break;
                        case OrbwalkerMode.Harass:
                            break;
                    }
                    KillStealLogic();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error in OnUpdate " + ex);
                }
            }
        }

        private static int GetEStackCount(AIBaseClient e)
        {
            if (e.HasBuff("TwitchDeadlyVenom"))
            {
                return e.GetBuffCount("TwitchDeadlyVenom");
            }
            else
            {
                return 0;
            }
        }

        private static float GetDamage(AIHeroClient target)
        {
            return E.GetDamage(target);
        }

        private static void KillStealLogic()
        {
            try
            {
                if (E.IsReady())
                    if (MainMenu["KillSteal"]["kse"].GetValue<MenuBool>().Enabled)
                        foreach (var e in ObjectManager.Get<AIHeroClient>().Where(e => e.IsValidTarget(E.Range)))
                            if (Me.GetSpellDamage(e, SpellSlot.E) > e.Health + 5)
                                E.Cast();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in KillSteal Logic " + ex);
            }
        }

        private static void Combo()
        {
            try
            {
                var e = TargetSelector.GetTarget(E.Range, E.DamageType);

                if (e != null)
                {
                    if (MainMenu["Combo"]["comboQ"].GetValue<MenuBool>().Enabled)
                    {
                        if (e.IsValidTarget(R.Range))
                            if (Q.IsReady())
                                Q.Cast();
                    }

                    if (MainMenu["Combo"]["comboW"].GetValue<MenuBool>().Enabled)
                    {
                        if (e.IsValidTarget(W.Range))
                            if (W.IsReady())
                                if (W.CanCast(e))
                                    Orbwalker.ResetAutoAttackTimer();
                        W.Cast(e, true);
                    }

                    if (MainMenu["Combo"]["comboE"].GetValue<MenuBool>().Enabled)
                    {
                        if (e.IsValidTarget(E.Range))
                        {
                            if (E.IsReady())
                            {
                                if (E.CanCast(e))
                                {
                                    if (MainMenu["Combo"]["comboEonlykill"].GetValue<MenuBool>().Enabled)
                                    {
                                        var damageetarget = GetDamage(e);
                                        if (damageetarget >= e.Health)
                                        {
                                            E.Cast();
                                        }
                                    }

                                    else if (GetEStackCount(e) >= 6)
                                    {
                                        E.Cast();
                                    }
                                }
                            }
                        }
                    }

                    if (MainMenu["Combo"]["comboR"].GetValue<MenuBool>().Enabled)
                    {
                        if (R.IsReady())
                            if (Me.CountEnemyHeroesInRange(R.Range + 100) >= MainMenu["Combo"]["comboRn"].GetValue<MenuSlider>())
                                R.Cast();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in Combo Logic " + ex);
            }
        }

        private static void LaneClear()
        {
            try
            {
                var e = TargetSelector.GetTarget(E.Range, E.DamageType);
                var w = TargetSelector.GetTarget(W.Range, W.DamageType);
            }

            catch (Exception ex)
            {
                Console.Write("Error in LaneClear Logic:" + ex);
            }
        }

        private static void JungleClear()
        {
            try
            {
                var e = TargetSelector.GetTarget(E.Range, E.DamageType);
                var w = TargetSelector.GetTarget(W.Range, W.DamageType);
            }

            catch (Exception ex)
            {
                Console.Write("Error in LaneClear Logic:" + ex);
            }
        }
    }
}
