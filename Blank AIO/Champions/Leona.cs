using System;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.MenuUI.Values;
using EnsoulSharp.SDK.Prediction;
using Support.Util;
using ActiveGapcloser = BlankAIO.Util.ActiveGapcloser;

namespace BlankAIO.Champions
{
    public class Leona
    {
        private static Spell Q, W, E, R;

        private static Menu LeonaMenu;
        private static AIHeroClient Player;

        public static void Load()
        {
            GameEvent.OnGameLoad += OnLoad;
            LeonaMenu = new Menu("BlankAIO", "BlankAIO - Leona", true);
        }

        public static void OnLoad()
        {
            Q = new Spell(SpellSlot.Q, 175);
            W = new Spell(SpellSlot.W, 125);
            E = new Spell(SpellSlot.E, 700);
            R = new Spell(SpellSlot.R, 1200);

            E.SetSkillshot(0.25f, 100f, 2000f, false, false, EnsoulSharp.SDK.Prediction.SkillshotType.Line);
            R.SetSkillshot(1f, 300f, float.MaxValue, false, false, EnsoulSharp.SDK.Prediction.SkillshotType.Circle);

            var comboMenu = new Menu("Combo", "Combo Settings");

            comboMenu.Add(new MenuBool("ComboE", "Use E without Q", false));
            comboMenu.Add(new MenuBool("ComboQWE", "Use Q/W/E", true));
            comboMenu.Add(new MenuBool("ComboR", "Use R"));

            LeonaMenu.Add(comboMenu);

            var interruptMenu = new Menu("Interrupt", "Interrupt Settings");
            interruptMenu.Add(new MenuBool("GapcloserQ", "Use Q to Interrupt Gapcloser", true));
            interruptMenu.Add(new MenuBool("InterruptQ", "Use Q to Interrupt Spells", true));
            interruptMenu.Add(new MenuBool("InterruptR", "Use R to Interrupt Spells", true));

            LeonaMenu.Add(interruptMenu);

            LeonaMenu.Attach();

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
                }
            }
        }

        static void Combo()
        {
            var Target = TargetSelector.GetTarget(R.Range, false);

            if (Q.CanCast(Target))
            {
                Orbwalker.ResetAutoAttackTimer();
                Player.IssueOrder(GameObjectOrder.AttackUnit, Target);
            }

            if (W.CanCast(Target))
            {
                W.Cast();
            }

            if (E.CanCast(Target) && Q.IsReady())
            {
                // Max Range with VeryHigh Hitchance / Immobile
                if (E.GetPrediction(Target).Hitchance >= HitChance.VeryHigh)
                {
                    if (E.CanCast(Target) && W.IsReady())
                    {
                        W.Cast();
                    }
                }

                // Lower Range
                if (E.GetPrediction(Target, false, 775).Hitchance >= HitChance.High)
                {
                    if (E.CanCast(Target) && W.IsReady())
                    {
                        W.Cast();
                    }
                }
            }

            if (E.CanCast(Target))
            {
                E.Cast(Target);
            }

            if (R.CanCast(Target))
            {
                R.CastIfHitchanceEquals(Target, HitChance.Immobile);
            }
        }

        private static void OnAfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (!unit.IsMe)
            {
                return;
            }

            if (!(target is AIHeroClient) && !target.Name.ToLower().Contains("ward"))
            {
                return;
            }

            if (!Q.IsReady())
            {
                return;
            }

            if (Q.Cast())
            {
                Orbwalker.ResetAutoAttackTimer();
                Player.IssueOrder(GameObjectOrder.AttackUnit, target);
            }
        }

        private static void OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (gapcloser.Sender.IsAlly)
            {
                return;
            }

            if (LeonaMenu["Interrupt Settings"]["GapcloserQ"].GetValue<MenuBool>().Enabled && E.IsReady())
            {

                if (Q.CanCast(gapcloser.Sender))
                {
                    if (Q.Cast())
                    {
                        Orbwalker.ResetAutoAttackTimer();
                        Player.IssueOrder(GameObjectOrder.AttackUnit, gapcloser.Sender);
                    }
                }
            }
        }

        private static void OnPossibleToInterrupt(AIHeroClient target, Interrupter.InterruptSpellArgs args)
        {
            if (args.DangerLevel < Interrupter.DangerLevel.High || target.IsAlly)
            {
                return;
            }

            if (LeonaMenu["Interrupt Settings"]["InterruptQ"].GetValue<MenuBool>().Enabled && Q.IsReady())
            {

                if (Q.CanCast(target))
                {
                    if (Q.Cast())
                    {
                        Orbwalker.ResetAutoAttackTimer();
                        Player.IssueOrder(GameObjectOrder.AttackUnit, target);
                    }

                    return;
                }
            }

            if (LeonaMenu["Interrupt Settings"]["InterruptR"].GetValue<MenuBool>().Enabled && R.IsReady())
            {

                if (R.CanCast(target))
                {
                    R.Cast(target);
                }
            }
        }
    }
}