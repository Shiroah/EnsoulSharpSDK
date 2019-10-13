using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.Utility;
using SharpDX;
using Color = System.Drawing.Color;

namespace BlankAIO.Champions.Helpers
{
    class PixManager
    {
        private static AIBaseClient _pix = null;

        public static bool DrawPix { get; set; }

        public const string PixObjectName = "RobotBuddy";

        public static AIBaseClient Pix
        {
            get
            {
                if (_pix != null && _pix.IsValid)
                {
                    return _pix;
                }

                return null;
            }
        }

        static PixManager()
        {
            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnDraw += Drawing_OnEndScene;

            _pix = ObjectManager.Get<AIBaseClient>().FirstOrDefault(o => o.IsAlly && o.Name == PixObjectName);
            if (Pix != null)
            {
                Console.Write(Pix.Name);
            }
        }

        static void Game_OnUpdate(EventArgs args)
        {
            if (Pix == null)
            {
                _pix = ObjectManager.Get<AIBaseClient>().FirstOrDefault(o => o.IsAlly && o.Name == PixObjectName);
            }
        }

        static void Drawing_OnEndScene(EventArgs args)
        {
            if (DrawPix)
            {
                if (Pix != null)
                {
                    Render.Circle.DrawCircle(Pix.Position + new Vector3(0, 0, 15), 100, Color.Purple, 5, true);
                }
            }
        }

    }
}
