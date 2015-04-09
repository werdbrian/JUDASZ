#region

using LeagueSharp.Common;

#endregion

namespace Vomar_Soraka
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += VomarSoraka.OnGameLoad;
        }
    }
}