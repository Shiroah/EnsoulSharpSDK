using System;
using System.Collections.Generic;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.MenuUI.Values;
using EnsoulSharp.SDK.Prediction;
using SharpDX;
using Color = System.Drawing.Color;

namespace BlankAIO.Champions
{
    class VelKoz
    {
        public const string ChampionName = "Velkoz";

        public static Spell Q;
        public static Spell QSplit;
        public static Spell QDummy;
        public static Spell W;
        public static Spell E;
        public static Spell R;

        public static SpellSlot IgniteSlot;

        public static MissileClient QMissile;

        public static Menu MainMenu;

        private static AIHeroClient Player;
        private static void Main(string[] args)
        {
            GameEvent.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad()
        {
            Player = ObjectManager.Player;

            Q = new Spell(SpellSlot.Q, 1200);
            QSplit = new Spell(SpellSlot.Q, 1100);
            QDummy = new Spell(SpellSlot.Q, (float)Math.Sqrt(Math.Pow(Q.Range, 2) + Math.Pow(QSplit.Range, 2)));
            W = new Spell(SpellSlot.W, 1200);
            E = new Spell(SpellSlot.E, 800);
            R = new Spell(SpellSlot.R, 1550);

            IgniteSlot = Player.GetSpellSlot("SummonerDot");


            Q.SetSkillshot(0.25f, 50f, 1300f, true, false, SkillshotType.Line);
            QSplit.SetSkillshot(0.25f, 55f, 2100, true, SkillshotType.Line);
            QDummy.SetSkillshot(0.25f, 55f, float.MaxValue, false, SkillshotType.Line);
            W.SetSkillshot(0.25f, 85f, 1700f, false, SkillshotType.Line);
            E.SetSkillshot(0.5f, 100f, 1500f, false, SkillshotType.Circle);
            R.SetSkillshot(0.3f, 1f, float.MaxValue, false, SkillshotType.Line);

            MainMenu = new Menu("BlankAIO", "Blank AIO - Vel'Koz");

            #region Combo Menu

            var comboMenu = new Menu("Combo", "Combo Settings");

            comboMenu.Add(new MenuBool("comboQ", "Use Q", true));
            comboMenu.Add(new MenuBool("comboW", "Use W", true));
            comboMenu.Add(new MenuBool("comboE", "Use E", true));
            comboMenu.Add(new MenuBool("comboR", "Use R", true));
            comboMenu.Add(new MenuBool("comboIGNITE", "Use Ignite", true));

            MainMenu.Add(comboMenu);

            #endregion

            #region Harass

            var harassMenu = new Menu("Harass", "Harass Settings");

            harassMenu.Add(new MenuBool("harassQ", "Use Q", true));
            harassMenu.Add(new MenuBool("harassW", "Use W", false));
            harassMenu.Add(new MenuBool("harassE", "Use E", false));

            MainMenu.Add(harassMenu);

            #endregion

            #region Lane Clear

            var laneclearMenu = new Menu("LaneClear", "Lane Clear Settings");

            laneclearMenu.Add(new MenuBool("laneclearQ", "Use Q", false));
            laneclearMenu.Add(new MenuBool("laneclearW", "Use W", false));
            laneclearMenu.Add(new MenuBool("laneclearE", "Use E", false));

            MainMenu.Add(laneclearMenu);

            #endregion

            #region Jungle Clear

            var jungleclearMenu = new Menu("LaneClear", "Lane Clear Settings");

            jungleclearMenu.Add(new MenuBool("jungleclearQ", "Use Q", false));
            jungleclearMenu.Add(new MenuBool("jungleclearW", "Use W", false));
            jungleclearMenu.Add(new MenuBool("jungleclearE", "Use E", false));

            MainMenu.Add(jungleclearMenu);

            #endregion

            #region R 

            var rMenu = new Menu("RSetting", "R Settings");

            rMenu.Add(new MenuSeparator("dontr", "Don't Use R on"));

            foreach (var enemy in ObjectManager.Get<AIHeroClient>().Where(enemy => enemy.Team != Player.Team))
            {
                rMenu.Add(new MenuBool("DontUlt" + enemy.CharacterName, enemy.CharacterName));
            }

            MainMenu.Add(rMenu);

            #endregion

            #region Misc

            var miscMenu = new Menu("Misc", "Misc Settings");

            miscMenu.Add(new MenuBool("Interrupt", "Interrupt Spells with E", true));

            MainMenu.Add(miscMenu);

            #endregion

            #region Drawings

            var drawMenu = new Menu("Draw", "Drawing Settings");

            drawMenu.Add(new MenuBool("drawQ", "Draw Q Range", true));
            drawMenu.Add(new MenuBool("drawW", "Draw W Range", true));
            drawMenu.Add(new MenuBool("drawE", "Draw E Range", true));
            drawMenu.Add(new MenuBool("drawR", "Draw R Range", true));

            MainMenu.Add(drawMenu);

            #endregion

            // Events, don't touch while copypasting this :P
            Game.OnTick += GameOnUpdate;
            Drawing.OnDraw += DrawingOnDraw;
            Interrupter.OnInterrupterSpell += InterrupterOnTarget;
            GameObject.OnCreate += GameObjectOnCreate;
            Chat.Print("Blank AIO, Vel'Koz loaded! This script is still in Beta, as every other one.");
        }

        static void InterrupterOnTarget(AIHeroClient sender, Interrupter.InterruptSpellArgs args)
        {
            if (MainMenu["Misc"]["Interrupt"].GetValue<MenuBool>().Enabled)
            {
                E.Cast(sender);
            }
        }

        private static void GameObjectOnCreate(GameObject sender, EventArgs args)
        {
            if (!(sender is MissileClient)) return;
            var missile = (MissileClient)sender;
            if (missile.SpellCaster != null && missile.SpellCaster.IsValid && missile.SpellCaster.IsMe && missile.SData.Name.Equals("VelkozQMissile", StringComparison.InvariantCultureIgnoreCase))
            {
                QMissile = missile;
            }
        }

        private static void Combo()
        {
            if (MainMenu["Combo"]["comboQ"].GetValue<MenuBool>().Enabled && Q.IsReady())
            {
                UseSpells(true, null, null, null);
            }
        }

        private static float GetComboDamage(AIBaseClient enemy)
        {
            var damage = 0d;

            if (Q.IsReady() && Q.GetCollision(ObjectManager.Player.Position.ToVector2(), new List<Vector2> { enemy.Position.ToVector2() }).Count == 0)
                damage += Player.GetSpellDamage(enemy, SpellSlot.Q);

            if (W.IsReady())
                damage += W.Instance.Ammo *
                          Player.GetSpellDamage(enemy, SpellSlot.W);

            if (E.IsReady())
                damage += Player.GetSpellDamage(enemy, SpellSlot.E);

            if (IgniteSlot != SpellSlot.Unknown && Player.Spellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
                damage += Player.GetSummonerSpellDamage(enemy, SummonerSpell.Ignite);

            if (R.IsReady())
                damage += 7 * Player.GetSpellDamage(enemy, SpellSlot.R) / 10;

            return (float)damage;
        }

        private static void UseSpells(bool useQ, bool useW, bool useE, bool useR, bool useIGNITE)
        {
            var qTarget = TargetSelector.GetTarget(Q.Range);
            var qDummyTarget = TargetSelector.GetTarget(QDummy.Range);
            var wTarget = TargetSelector.GetTarget(W.Range);
            var eTarget = TargetSelector.GetTarget(E.Range);
            var rTarget = TargetSelector.GetTarget(R.Range);

            if (useW && wTarget != null && W.IsReady())
            {
                W.Cast(wTarget);
                return;
            }

            if (useE && eTarget != null && E.IsReady())
            {
                E.Cast(eTarget);
                return;
            }

            if (qDummyTarget != null && useQ && Q.IsReady() && Q.Instance.ToggleState == 0)
            {
                if (qTarget != null) qDummyTarget = qTarget;
                QDummy.Delay = Q.Delay + Q.Range / Q.Speed * 1000 + QSplit.Range / QSplit.Speed * 1000;

                var predictedPos = QDummy.GetPrediction(qDummyTarget);
                if (predictedPos.Hitchance >= HitChance.High)
                {
                    for (var i = -1; i < 1; i = i + 2)
                    {
                        var alpha = 28 * (float)Math.PI / 180;
                        var cp = ObjectManager.Player.Position.ToVector2() +
                                 (predictedPos.CastPosition.ToVector2() - ObjectManager.Player.Position.ToVector2()).Rotated
                                     (i * alpha);
                        if (
                            Q.GetCollision(ObjectManager.Player.Position.ToVector2(), new List<Vector2> { cp }).Count ==
                            0 &&
                            QSplit.GetCollision(cp, new List<Vector2> { predictedPos.CastPosition.ToVector2() }).Count == 0)
                        {
                            Q.Cast(cp);
                            return;
                        }
                    }
                }
            }

            if (qTarget != null && useIGNITE && IgniteSlot != SpellSlot.Unknown &&
                Player.Spellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
            {
                if (Player.Distance(qTarget) < 650 && GetComboDamage(qTarget) > qTarget.Health)
                {
                    Player.Spellbook.CastSpell(IgniteSlot, qTarget);
                }
            }

            if (useR && rTarget != null && R.IsReady() &&
                Player.GetSpellDamage(rTarget, SpellSlot.R) / 10 * (Player.Distance(rTarget) < (R.Range - 500) ? 10 : 6) > rTarget.Health)
            {
                R.Cast(rTarget);
            }
        }

        private static void GameOnUpdate()
        {
            if (Player.IsDead) return;
            if (Player.IsCastingImporantSpell())
            {
                var endPoint = new Vector2();
                foreach (var obj in ObjectManager.Get<GameObject>())
                {
                    if (obj != null && obj.IsValid && obj.Name.Contains("Velkoz_") &&
                        obj.Name.Contains("_R_Beam_End"))
                    {
                        endPoint = Player.Position.ToVector2() +
                                   R.Range * (obj.Position - Player.Position).ToVector2().Normalized();
                        break;
                    }
                }

                if (endPoint.IsValid())
                {
                    var targets = new List<AIBaseClient>();

                    foreach (var enemy in ObjectManager.Get<AIHeroClient>().Where(h => h.IsValidTarget(R.Range)))
                    {
                        if (enemy.Position.ToVector2().Distance(Player.Position.ToVector2(), endPoint, true) < 400)
                            targets.Add(enemy);
                    }
                    if (targets.Count > 0)
                    {
                        var target = targets.OrderBy(t => t.Health / Q.GetDamage(t)).ToList()[0];
                        ObjectManager.Player.Spellbook.UpdateChargeableSpell(SpellSlot.R, target.Position, false, false);
                    }
                    else
                    {
                        ObjectManager.Player.Spellbook.UpdateChargeableSpell(SpellSlot.R, Game.CursorPosCenter, false, false);
                    }
                }

                return;
            }


            if (QMissile != null && QMissile.IsValid && Q.Instance.ToggleState == 2)
            {
                var qMissilePosition = QMissile.Position.ToVector2();
                var perpendicular = (QMissile.EndPosition - QMissile.StartPosition).ToVector2().Normalized().Perpendicular();

                var lineSegment1End = qMissilePosition + perpendicular * QSplit.Range;
                var lineSegment2End = qMissilePosition - perpendicular * QSplit.Range;

                var potentialTargets = new List<AIBaseClient>();
                foreach (
                    var enemy in
                        ObjectManager.Get<AIHeroClient>()
                            .Where(
                                h =>
                                    h.IsValidTarget() &&
                                    h.Position.ToVector2()
                                        .Distance(qMissilePosition, QMissile.EndPosition.ToVector2(), true) < 700))
                {
                    potentialTargets.Add(enemy);
                }

                QSplit.UpdateSourcePosition(qMissilePosition.ToVector3(), qMissilePosition.ToVector3());

                foreach (
                    var enemy in
                        ObjectManager.Get<AIHeroClient>()
                            .Where(
                                h =>
                                    h.IsValidTarget() &&
                                    (potentialTargets.Count == 0 ||
                                     h.NetworkId == potentialTargets.OrderBy(t => t.Health / Q.GetDamage(t)).ToList()[0].NetworkId) &&
                                    (h.Position.ToVector2().Distance(qMissilePosition, QMissile.EndPosition.ToVector2(), true) > Q.Width + h.BoundingRadius)))
                {
                    var prediction = QSplit.GetPrediction(enemy);
                    var d1 = prediction.UnitPosition.ToVector2().Distance(qMissilePosition, lineSegment1End, true);
                    var d2 = prediction.UnitPosition.ToVector2().Distance(qMissilePosition, lineSegment2End, true);
                    if (prediction.Hitchance >= HitChance.High &&
                        (d1 < QSplit.Width + enemy.BoundingRadius || d2 < QSplit.Width + enemy.BoundingRadius))
                    {
                        Q.Cast();
                    }
                }
            }

            // ORBWALKER HERE
        }


    }
}
