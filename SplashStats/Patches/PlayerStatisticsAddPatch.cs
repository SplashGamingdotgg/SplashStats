using System;
using Harmony;
using UnityEngine;

namespace SplashStats.Patches
{
    [HarmonyPatch(typeof(PlayerStatistics), "Add")]
    public class PlayerStatisticsAddPatch
    {
        public static void Prefix(string name, int val, Stats stats, PlayerStatistics __instance)
        {   
            // TODO: Do some stuff here maybe for extra stats. It's free real estate.
        }
    }
}