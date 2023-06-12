using System;
using Harmony;
using UnityEngine;

namespace SplashStats.Patches
{
    [HarmonyPatch(typeof(BasePlayer), "BecomeWounded")]
    public class BasePlayerBecomeWoundedPatch
    {
        public static void Postfix(HitInfo info, BasePlayer __instance)
        {
            try
            {
                StatManager.ProcessWounded(__instance, info);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Exception running BasePlayer.RecoverFromWounded hook: {e}");
                Debug.LogException(e);
            }
        }       
    }
}