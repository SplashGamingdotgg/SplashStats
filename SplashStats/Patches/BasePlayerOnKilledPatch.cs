using System;
using Harmony;
using UnityEngine;

namespace SplashStats.Patches
{
    [HarmonyPatch(typeof(BasePlayer), "OnKilled")]
    public class BasePlayerOnKilledPatch
    {
        public static void Prefix(HitInfo info, BasePlayer __instance)
        {
            try
            {
                StatManager.ProcessOnKilled(__instance, info);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Exception running BasePlayer.OnKilled hook: {e}");
            }
        }
    }
}