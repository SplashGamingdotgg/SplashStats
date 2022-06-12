using System.Collections.Generic;
using ConVar;
using UnityEngine;
using SplashUtilities;

namespace SplashStats
{
    public class BaseKillData
    {
        public string AttackerName;
        public ulong AttackerID;
        public string VictimName;
        public ulong VictimID;
        public string BoneName;

        [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public HitArea BoneArea;

        public float ProjectileDistance;
        public string ProjectileName;
        public string WeaponName;
    }

    public class PlayerKillData : BaseKillData
    {
        public string AssistName;
        public ulong AssistID;

        public static T FromHitInfo<T>(BasePlayer victim, HitInfo info) where T : BaseKillData, new()
        {
            var data = new T()
            {
                VictimName = victim.displayName,
                VictimID = victim.userID,
                BoneName = info.boneName,
                BoneArea = info.boneArea,
                ProjectileDistance = info.ProjectileDistance,
                ProjectileName = info.ProjectilePrefab?.ToString(),
                WeaponName = info.Weapon?.ShortPrefabName
            };

            if (info.InitiatorPlayer == null)
            {
                data.AttackerName = "N/A";
                data.AttackerID = 0;
            }
            else
            {
                data.AttackerName = info.InitiatorPlayer.displayName;
                data.AttackerID = info.InitiatorPlayer.userID;
            }

            return data;
        }
    }

    public class WoundData : BaseKillData
    {
    }
    
    public class StatManager : SingletonComponent<StatManager>
    {
        public static Dictionary<ulong, WoundData> WoundData = new Dictionary<ulong, WoundData>();

        public static void HelloWorld()
        {
            Debug.Log("Called from Oxide Plugin, but this is run in a Harmony DLL: Hello World");
        }

        public static void ProcessWounded(BasePlayer victim, HitInfo info)
        {
            if (victim == null)
            {
                Debug.LogWarning($"Victim was null in ProcessWounded");
                return;
            }

            WoundData[victim.userID] = PlayerKillData.FromHitInfo<WoundData>(victim, info); 
        }

        public static void ProcessRecovery(BasePlayer victim)
        {
            if (victim == null)
            {
                Debug.LogWarning($"Victim was null in ProcessRecovery");
                return;
            }

            if (WoundData.ContainsKey(victim.userID))
            {
                WoundData.Remove(victim.userID);
            }
        }

        public static void ProcessOnKilled(BasePlayer victim, HitInfo info)
        {
            if (victim == null)
            {
                Debug.LogWarning($"Victim was null in ProcessOnKilled, this should never happen.");
                return;
            }
            
            if (info == null)
            {
                Debug.LogWarning($"Victim {victim} had a null HitInfo in OnKilled?");
                return;
            }
            
            Debug.Log($"Processing OnKilled for {victim.displayName}, {info}");
            
            var killdata = PlayerKillData.FromHitInfo<PlayerKillData>(victim, info);
            WoundData woundata;
            
            if (WoundData.TryGetValue(victim.userID, out woundata))
            {
                killdata.AssistName = woundata.AttackerName;
                killdata.AssistID = woundata.AttackerID;

                WoundData.Remove(victim.userID);
            }

            RustUtils.BroadcastCustom("PlayerKill", killdata);
        }
    }
}
