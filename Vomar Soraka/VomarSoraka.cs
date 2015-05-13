﻿#region

using System;
using System.Collections;
using System.Linq;
using Color = System.Drawing.Color;
using System.Collections.Generic;
using System.Threading;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

#endregion

namespace Vomar_Soraka
{
    internal class VomarSoraka
    {
        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;
        public static Menu Menu;
        public static Orbwalking.Orbwalker Orbwalker;
        public static bool Packets
        {
            get { return Menu.Item("packets").GetValue<bool>(); }
        }
        public static void OnGameLoad(EventArgs args)
        {
            if (ObjectManager.Player.ChampionName != "Soraka")
            {
                return;
            }
            Q = new Spell(SpellSlot.Q, 950);
            W = new Spell(SpellSlot.W, 550);
            E = new Spell(SpellSlot.E, 925);
            R = new Spell(SpellSlot.R);
            Q.SetSkillshot(0.5f, 300, 1750, false, SkillshotType.SkillshotCircle);
            E.SetSkillshot(0.5f, 70f, 1750, false, SkillshotType.SkillshotCircle);
            CreateMenu();
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Interrupter2.OnInterruptableTarget += Interrupter2_OnInterruptableTarget;
            Game.OnUpdate += GameOnOnGameUpdate;
            Drawing.OnDraw += DrawingOnOnDraw;
        }
        private static void DrawingOnOnDraw(EventArgs args)
        {
            var drawQ = Menu.Item("drawQ").GetValue<bool>();
            var drawW = Menu.Item("drawW").GetValue<bool>();
            var drawE = Menu.Item("drawE").GetValue<bool>();
            var p = ObjectManager.Player.Position;
            if (drawQ)
            {
                Render.Circle.DrawCircle(p, Q.Range, Q.IsReady() ? Color.Aqua : Color.Red);
            }
            if (drawW)
            {
                Render.Circle.DrawCircle(p, W.Range, W.IsReady() ? Color.Aqua : Color.Red);
            }
            if (drawE)
            {
                Render.Circle.DrawCircle(p, E.Range, E.IsReady() ? Color.Aqua : Color.Red);
            }
        }
        private static void GameOnOnGameUpdate(EventArgs args)
        {
            switch (Orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Mixed:
                    Harass();
					LaneFarm();
					JungleFarm();
                    break;
                case Orbwalking.OrbwalkingMode.Combo:
                    Combo();
                    break;
				case Orbwalking.OrbwalkingMode.LaneClear:
					LaneFarm();
					JungleFarm();
                    break;					
            }
			if (Menu.Item("SmartKs").GetValue<bool>())
            {
                SmartKs();
            }
            if (Menu.Item("autoW").GetValue<bool>())
            {
                AutoW();
            }
            if (Menu.Item("autoR").GetValue<bool>())
            {
                AutoR();
            }
        }
        private static void AutoR()
        {
            if (!R.IsReady())
            {
                return;
            }
            foreach (var friend in
                ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsAlly).Where(x => !x.IsDead).Where(x => !x.IsZombie))
            {
                var friendHealth = (int) friend.Health / friend.MaxHealth * 100;
                var health = Menu.Item("autoRPercent").GetValue<Slider>().Value;

                if (friendHealth <= health)
                {
                    R.Cast(Packets);
                }
            }
        }
        private static void AutoW()
        {
            if (!W.IsReady())
            {
                return;
            }
            var autoWHealth = Menu.Item("autoWHealth").GetValue<Slider>().Value;
            if (ObjectManager.Player.HealthPercentage() < autoWHealth)
            {
                return;
            }
            foreach (var friend in
                from friend in
                    ObjectManager.Get<Obj_AI_Hero>()
                        .Where(x => !x.IsEnemy)
                        .Where(x => !x.IsMe)
                        .Where(friend => W.IsInRange(friend.ServerPosition, W.Range))
                let friendHealth = friend.Health / friend.MaxHealth * 100
                let healthPercent = Menu.Item("autoWPercent").GetValue<Slider>().Value
                where friendHealth <= healthPercent
                select friend)
            {
                W.CastOnUnit(friend, Packets);
            }
        }
        private static void Combo()
        {
            var useQ = Menu.Item("useQ").GetValue<bool>();
            var useE = Menu.Item("useE").GetValue<bool>();
            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);

            if (target == null)
            {
                return;
            }
            if (useQ && Q.IsReady())
            {
                Q.Cast(target, Packets);
            }

            if (useE && E.IsReady())
            {
                E.Cast(target, Packets);
            }
        }
        private static void Harass()
        {
            var useQ = Menu.Item("useQHarass").GetValue<bool>();
            var useE = Menu.Item("useEHarass").GetValue<bool>();
            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
            if (target == null)
            {
                return;
            }
            if (useQ && Q.IsReady())
            {
                Q.Cast(target, Packets);
            }
            if (useE && E.IsReady())
            {
                E.Cast(target, Packets);
            }
        }
		
        public static HitChance GetHitchance()
        {
            switch (Menu.Item("vHitchance").GetValue<StringList>().SelectedIndex)
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
                    return HitChance.Medium;
            }
        }

		private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (!Menu.Item("AntiGapCloser").GetValue<bool>() || !VomarSoraka.E.CanCast(gapcloser.Sender))
                return;

            VomarSoraka.E.Cast(gapcloser.Sender);
        }

        private static void Interrupter2_OnInterruptableTarget(Obj_AI_Hero sender, Interrupter2.InterruptableTargetEventArgs args)
        {
            if (!Menu.Item("InterruptSpells").GetValue<bool>() || !VomarSoraka.E.CanCast(sender) || args.DangerLevel != Interrupter2.DangerLevel.High)
                return;

            VomarSoraka.E.Cast(sender);
        }
		
		private static void JungleFarm()
        {
            if (Menu.Item("UseQJungle").GetValue<bool>() && Q.IsReady() )
            {
                var JungleWMinions = MinionManager.GetMinions(ObjectManager.Player.Position, Q.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
                List<Vector2> minionerinos2 =
         (from minions in JungleWMinions select minions.Position.To2D()).ToList();
                var ePos2 = MinionManager.GetBestCircularFarmLocation(minionerinos2, Q.Width, Q.Range).Position;
                if (ePos2.Distance(ObjectManager.Player.Position.To2D()) < Q.Range && MinionManager.GetBestCircularFarmLocation(minionerinos2, Q.Width, Q.Range).MinionsHit > 0)
                {
                    Q.Cast(ePos2, Packets);
                }
            }
        }
		private static void LaneFarm()
        {
			if (Menu.Item("UseQLane").GetValue<bool>() && Q.IsReady())
			{
				var laneMinions = MinionManager.GetMinions(ObjectManager.Player.Position, Q.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.MaxHealth);
				List<Vector2> minionerinos2 =
		(from minions in laneMinions select minions.Position.To2D()).ToList();
				var ePos2 = MinionManager.GetBestCircularFarmLocation(minionerinos2, Q.Width, Q.Range).Position;
				if (ePos2.Distance(ObjectManager.Player.Position.To2D()) < Q.Range && MinionManager.GetBestCircularFarmLocation(minionerinos2, Q.Width, Q.Range).MinionsHit > 0)
				{
					Q.Cast(ePos2, Packets);
				}
			}
		}
		
		private static void SmartKs()
        {
            if (!Menu.Item("smartKS", true).GetValue<bool>())
                return;

            foreach (Obj_AI_Hero target in ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsValidTarget(E.Range) && !x.IsDead && !x.HasBuffOfType(BuffType.Invulnerability)))
            {
				if (E.IsKillable(target) && ObjectManager.Player.Distance(target.Position) < E.Range)
                {
                    E.Cast(target);
                }
                if (Q.IsKillable(target) && ObjectManager.Player.Distance(target.Position) < Q.Range)
                {
					var prediction = Q.GetPrediction(target);
                    if (prediction.Hitchance >= HitChance.High)
					{
                    Q.Cast(target);
					}
                }
            }
        }
		
		private static double GetTeamHp
        {
            get
            {
                return HeroManager.Allies.Where(allies => allies.IsValidTarget(float.MaxValue, false)).Select(allies => allies.HealthPercentage()).FirstOrDefault();
            }
        }

        internal static void TeamfightUltimate()
        {
            if (!Menu.Item("useTeamfightUltimate").GetValue<bool>() || !R.IsReady())
                return;

            if (GetTeamHp < Menu.Item("percentage2").GetValue<Slider>().Value)
            {
                R.Cast();
            }
        }
		
        private static void CreateMenu()
        {
            Menu = new Menu("Vomar Soraka", "vSoraka", true);
            var tsMenu = new Menu("Target Selector", "ssTS");
            TargetSelector.AddToMenu(tsMenu);
            Menu.AddSubMenu(tsMenu);
            var orbwalkingMenu = new Menu("Orbwalking", "vOrbwalking");
            Orbwalker = new Orbwalking.Orbwalker(orbwalkingMenu);
            Menu.AddSubMenu(orbwalkingMenu);
            var comboMenu = new Menu("Combo", "vCombo");
            comboMenu.AddItem(new MenuItem("useQ", "Use Q").SetValue(true));
            comboMenu.AddItem(new MenuItem("useE", "Use E").SetValue(true));
			comboMenu.AddItem(new MenuItem("smartKS", "Use Smart KS System", true).SetValue(true));
			comboMenu.AddItem(new MenuItem("vHitchance", "Hitchance").SetValue(new StringList(new[] { "Low", "Medium", "High", "Very High" }, 2)));			
            Menu.AddSubMenu(comboMenu);
			var farmMenu = new Menu("Farm", "vFarm");
            farmMenu.AddItem(new MenuItem("UseQJungle", "Use Q Jungle").SetValue(true));
            farmMenu.AddItem(new MenuItem("UseQLane", "Use Q Lane").SetValue(true));
			farmMenu.AddItem(new MenuItem("ManaSliderFarm", "Mana To Farm").SetValue(new Slider(25, 100, 0)));
			Menu.AddSubMenu(farmMenu);
            var harassMenu = new Menu("Harass", "vHarass");
            harassMenu.AddItem(new MenuItem("useQHarass", "Use Q").SetValue(true));
            harassMenu.AddItem(new MenuItem("useEHarass", "Use E").SetValue(true));
            Menu.AddSubMenu(harassMenu);
            var drawingMenu = new Menu("Drawing", "vDrawing");
            drawingMenu.AddItem(new MenuItem("drawQ", "Draw Q").SetValue(true));
            drawingMenu.AddItem(new MenuItem("drawW", "Draw W").SetValue(true));
            drawingMenu.AddItem(new MenuItem("drawE", "Draw E").SetValue(true));
            Menu.AddSubMenu(drawingMenu);
            var healingMenu = new Menu("Healing", "vHealing");
			healingMenu.AddItem(new MenuItem("autoW", "Auto use W.").SetValue(true));
			healingMenu.AddItem(new MenuItem("autoR", "Auto use R.").SetValue(true));
			healingMenu.AddItem(new MenuItem("useTeamfightUltimate", "Auto teamfight R.").SetValue(true));
			healingMenu.AddItem(new MenuItem("percentage2", "Auto R under team %").SetValue(new Slider(60, 1)));
            healingMenu.AddItem(new MenuItem("autoWPercent", "Auto W under %").SetValue(new Slider(50, 1)));
            healingMenu.AddItem(new MenuItem("autoWHealth", "My Health Percent").SetValue(new Slider(40, 1)));
			healingMenu.AddItem(new MenuItem("autoRPercent", "Auto R under %").SetValue(new Slider(40, 1)));
			Menu.AddSubMenu(healingMenu);
            var miscMenu = new Menu("Misc", "vMisc");
            miscMenu.AddItem(new MenuItem("packets", "Use Packets").SetValue(true));
            miscMenu.AddItem(new MenuItem("AntiGapCloser", "E on Gapcloser").SetValue(true));
            miscMenu.AddItem(new MenuItem("InterruptSpells", "Use E to Interrupt").SetValue(true));
            Menu.AddSubMenu(miscMenu);
            Menu.AddToMainMenu();
        }
        public static void PrintChat(string msg)
        {
            Game.PrintChat("<font color='#3492EB'>Vomars Soraka:</font> <font color='#FFFFFF'>" + msg + "</font>");
        }
    }
}
