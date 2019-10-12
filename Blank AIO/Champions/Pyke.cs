using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.MenuUI.Values;
using EnsoulSharp.SDK.Prediction;
using EnsoulSharp.SDK.Utility;
using SharpDX;
using SPrediction;

namespace BlankAIO.Champions
{
    class Pyke
    {
        private static string ChampionName = "Pyke";
        private static AIHeroClient Player;
        private static Menu MainMenu;

        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;

        public static SpellSlot IgniteSlot;

        public static void Load()
        {
            GameEvent.OnGameLoad += OnLoad;
            MainMenu = new Menu("BlankAIO", "BlankAIO - Pyke", true);
        }

        public static void OnLoad()
        {
            Player = ObjectManager.Player;

            Q = new Spell(SpellSlot.Q, 1100);
            Q.SetCharged("PykeQ", "PykeQ", 400, 1030, 1.0f);
            Q.SetSkillshot(0.25f, 120f, 1700, true, false, SkillshotType.Line);
            W = new Spell(SpellSlot.W, float.MaxValue);
            E = new Spell(SpellSlot.E, 550);
            E.SetSkillshot(0.275f, 70f, 500f, false, false, SkillshotType.Line);
            R = new Spell(SpellSlot.R, 750);
            R.SetSkillshot(0.25f, 100f, float.MaxValue, false, false, SkillshotType.Circle);

            Chat.Print("BlankAIO.Pyke Loaded! This version is using the SDK orbwalker, please select 'SDK' with Orbwalker Selector.");

            #region Combo Menu

            var comboMenu = new Menu("Combo", "Combo Settings");

            comboMenu.Add(new MenuBool("comboQ", "Use Q", true));
            comboMenu.Add(new MenuSlider("comboQhc", "Q Hitchance (1-Low, 4-Very High)", 3, 1, 4));
            comboMenu.Add(new MenuBool("comboE", "Use E", true));
            comboMenu.Add(new MenuBool("comboR", "Use R", true));
            comboMenu.Add(new MenuBool("comboRonlykill", "^ Only if target is executable", true));

            MainMenu.Add(comboMenu);

            #endregion

            #region Farm Menu

            var laneclearMenu = new Menu("LaneClear", "Lane Clear Settings");

            laneclearMenu.Add(new MenuBool("laneclearE", "Use E", false));
            laneclearMenu.Add(new MenuSlider("eminions", "^ Minimum minions hit", 3, 1, 6));

            MainMenu.Add(laneclearMenu);

            #endregion

            #region Jungle Farm Menu

            var jungleclearMenu = new Menu("LaneClear", "Lane Clear Settings");

            jungleclearMenu.Add(new MenuBool("jungleclearQ", "Use Q", true));
            jungleclearMenu.Add(new MenuBool("jungleclearE", "Use E", true));

            MainMenu.Add(jungleclearMenu);

            #endregion

            #region Harass Menu

            var harassMenu = new Menu("Harass", "Harass Settings");

            harassMenu.Add(new MenuBool("harassQ", "Use Q", true));
            harassMenu.Add(new MenuBool("harassE", "Use E", false));

            MainMenu.Add(harassMenu);

            #endregion

            #region KillSteal Menu

            var killstealMenu = new Menu("KillSteal", "KillSteal Settings");

            killstealMenu.Add(new MenuBool("ksr", "Use R"));

            MainMenu.Add(killstealMenu);

            #endregion

            #region Drawing Menu



            #endregion

            MainMenu.Attach();

            Game.OnTick += Game_OnUpdate;
        }

        static void Game_OnUpdate(EventArgs args)
        {
            if (Player != null)
            {
                switch (Orbwalker.ActiveMode)
                {
                    case OrbwalkerMode.Combo:
                        Combo();
                        break;
                    case OrbwalkerMode.LaneClear:
                        Clear();
                        break;
                    case OrbwalkerMode.Harass:
                        Harass();
                        break;
                }
                if (MainMenu["KillSteal"]["ksr"].GetValue<MenuBool>().Enabled && R.IsReady())
                    KillSteal();
            }
        }

        private static void KillSteal()
        {
            var enemies = GameObjects.EnemyHeroes.Where(x => !x.IsDead && x.IsEnemy && !x.IsInvulnerable && x.Health < R.GetDamage(x, DamageStage.Empowered) && x.DistanceToPlayer() < R.Range);
            var t = enemies.FirstOrDefault(x => x.IsValidTarget(R.Range));
            if (t != null && !ObjectManager.Player.IsRecalling())
            {
                if (Orbwalker.ActiveMode != OrbwalkerMode.Combo && !t.IsDead && !t.IsZombie && t.IsVisible && t.IsHPBarRendered)
                {
                    R.SPredictionCast(t, HitChance.Medium);
                }
            }
            if (t == null)
            {
                return;
            }
        }

        private static void Combo()
        {
            var qhitchance = MainMenu["Combo"]["comboQhc"].GetValue<MenuSlider>();
            var qhitpred = HitChance.Medium;

            switch (qhitchance)
            {
                case 1:
                    qhitpred = HitChance.Low;
                    break;
                case 2:
                    qhitpred = HitChance.Medium;
                    break;
                case 3:
                    qhitpred = HitChance.High;
                    break;
                case 4:
                    qhitpred = HitChance.VeryHigh;
                    break;
            }

            if (MainMenu["Combo"]["comboQ"].GetValue<MenuBool>().Enabled && Q.IsReady())
            {
                var target = TargetSelector.GetTarget(Q.ChargedMaxRange);
                if (target != null && target.IsValidTarget(Q.ChargedMaxRange))
                {
                    var pred = Q.GetSPrediction(target);
                    if (pred.HitChance >= qhitpred)
                    {
                        Q.StartCharging();
                    }
                }
                if (target == null)
                {
                    return;
                }
            }

            if (MainMenu["Combo"]["comboQ"].GetValue<MenuBool>().Enabled && Q.IsReady() && Q.IsCharging)
            {
                var target = TargetSelector.GetTarget(Q.Range);
                if (target != null && target.IsValidTarget(Q.Range))
                {
                    var pred = Q.GetSPrediction(target);
                    if (pred.HitChance >= qhitpred)
                    {
                        Q.ShootChargedSpell(pred.CastPosition);
                    }
                }
                if (target == null)
                {
                    return;
                }
            }

            if (MainMenu["Combo"]["comboR"].GetValue<MenuBool>().Enabled && R.IsReady())
            {
                var rtarget = TargetSelector.GetTarget(R.Range);
                if (rtarget != null && rtarget.IsValidTarget(R.Range))
                {
                    if (MainMenu["Combo"]["comboRonlykill"].GetValue<MenuBool>().Enabled && rtarget.Health > R.GetDamage(rtarget, DamageStage.Empowered))
                    {
                        return;
                    }
                    if (!rtarget.IsDead && !rtarget.IsZombie && rtarget.IsVisible && rtarget.IsHPBarRendered)
                    {
                        R.SPredictionCast(rtarget, HitChance.High);
                    }
                }
                if (rtarget == null)
                {
                    return;
                }
            }

            if (MainMenu["Combo"]["comboE"].GetValue<MenuBool>().Enabled && !Q.IsCharging && E.IsReady())
            {
                var target = TargetSelector.GetTarget(E.Range);
                if (target != null && target.IsValidTarget(E.Range))
                {
                    var pred = E.GetSPrediction(target);
                    if (pred.HitChance >= HitChance.High)
                    {
                        E.SPredictionCast(target, HitChance.High);
                    }
                }
                if (target == null)
                {
                    return;
                }
            }
        }
        private static void Clear()
        {
            if (MainMenu["Lane Clear"]["laneclearE"].GetValue<MenuBool>().Enabled && E.IsReady())
            {
                var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(E.Range) && x.IsMinion())
                            .Cast<AIBaseClient>().ToList();

                if (minions.Any())
                {
                    var eFarmLocation = E.GetLineFarmLocation(minions);
                    if (eFarmLocation.Position.IsValid() && eFarmLocation.MinionsHit >= MainMenu["Lane Clear Settings"]["eminions"].GetValue<MenuSlider>())
                    {
                        E.Cast(eFarmLocation.Position);
                    }
                }
            }
        }

        private static void Harass()
        {
            var qhitchance = MainMenu["Combo"]["comboQhc"].GetValue<MenuSlider>();
            var qhitpred = HitChance.Medium;
            switch (qhitchance)
            {
                case 1:
                    qhitpred = HitChance.Low;
                    break;
                case 2:
                    qhitpred = HitChance.Medium;
                    break;
                case 3:
                    qhitpred = HitChance.High;
                    break;
                case 4:
                    qhitpred = HitChance.VeryHigh;
                    break;
            }
            if (Q.IsReady() && MainMenu["Combo"]["comboQ"].GetValue<MenuBool>().Enabled)
            {
                var target = TargetSelector.GetTarget(Q.ChargedMaxRange);
                if (target != null && target.IsValidTarget(Q.ChargedMaxRange))
                {
                    var pred = Q.GetPrediction(target);
                    if (pred.Hitchance >= qhitpred)
                    {
                        Q.StartCharging();
                    }
                }
                if (target == null)
                {
                    return;
                }
            }
            if (Q.IsReady() && Q.IsCharging)
            {
                var target = TargetSelector.GetTarget(Q.Range);
                if (target != null && target.IsValidTarget(Q.Range))
                {
                    var pred = Q.GetSPrediction(target);
                    if (pred.HitChance >= HitChance.High)
                    {
                        Q.ShootChargedSpell(pred.CastPosition);
                    }
                }
                if (target == null)
                {
                    return;
                }
            }
        }
    }
}