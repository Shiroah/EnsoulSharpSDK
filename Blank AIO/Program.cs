using System;
using System.Reflection;
using EnsoulSharp;
using EnsoulSharp.SDK;
using BlankAIO.Champions;

namespace BlankAIO
{
    internal class Program
    {
        public static AIHeroClient player;

        private static void Main(string[] args)
        {
            GameEvent.OnGameLoad += OnGameLoad;
        }

        private static void OnGameLoad()
        {
            try
            {
                player = ObjectManager.Player;
                switch (player.CharacterName)
                {
                    case "Twitch":
                        Twitch.Load();
                        break;
                    case "Leona":
                        Leona.Load();
                        break;
                    case "Pyke":
                        Pyke.Load();
                        break;                   
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed To load: " + e);
            }
        }
    }
}
