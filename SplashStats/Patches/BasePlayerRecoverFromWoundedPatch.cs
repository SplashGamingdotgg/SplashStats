using System;
using Harmony;
using UnityEngine;

namespace SplashStats.Patches
{
    [HarmonyPatch(typeof(BasePlayer), "RecoverFromWounded")]
    public class BasePlayerRecoverFromWoundedPatch
    {
        public static void Postfix(BasePlayer __instance)
        {
            try
            {
                StatManager.ProcessRecovery(__instance);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Exception running BasePlayer.RecoverFromWounded hook: {e}");
            }
        }       
    }
}