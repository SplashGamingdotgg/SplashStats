using System;
using System.Collections.Generic;
using ConVar;
using Rust;
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
        public int Distance;

        [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public HitArea BoneArea;

        public float ProjectileDistance;
        public string ProjectileName;
        public string WeaponName;

        [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public DamageType DamageType;
        
        public static T FromHitInfo<T>(BasePlayer victim, HitInfo info) where T : BaseKillData, new()
        {
            var data = new T()
            {
                BoneName = info.boneName,
                BoneArea = info.boneArea,
                ProjectileDistance = info.ProjectileDistance,
                ProjectileName = info.ProjectilePrefab?.name,
                WeaponName = info.WeaponPrefab?.ShortPrefabName,
                DamageType =  info.damageTypes.GetMajorityDamageType()
            };

            if (victim.IsNpc)
            {
                data.VictimName = victim.displayName;
                data.VictimID = 1;
            }
            else
            {
                data.VictimName = victim.displayName;
                data.VictimID = victim.userID;
            }

            if (info.Initiator != null && victim != null)
            {
                data.Distance = Convert.ToInt32(Vector3.Distance(victim.transform.position, info.Initiator.transform.position));
            }
            
            if (info.InitiatorPlayer == null)
            {
                if (info.Initiator == null)
                {
                    data.AttackerName = "N/A";
                    data.AttackerID = 0;
                }
                else
                {
                    data.AttackerName = info.Initiator.ShortPrefabName;
                    data.AttackerID = 2;
                }
            }
            else
            {
                if (info.InitiatorPlayer.IsNpc)
                {
                    // Maybe we use this? I'm not sure.
                    data.AttackerName = info.InitiatorPlayer.displayName;
                    data.AttackerID = 1;
                }
                else
                {
                    data.AttackerName = info.InitiatorPlayer.displayName;
                    data.AttackerID = info.InitiatorPlayer.userID;
                }
            }

            return data;
        }
    }

    public class PlayerKillData : BaseKillData
    {
        public string AssistName;
        public ulong AssistID;

        public static PlayerKillData FromWoundData(WoundData data)
        {
            return new PlayerKillData
            {
                AttackerName = data.AttackerName,
                AttackerID = data.AttackerID,
                VictimName = data.VictimName,
                VictimID = data.VictimID,
                BoneName = data.BoneName,
                BoneArea = data.BoneArea,
                ProjectileDistance = data.ProjectileDistance,
                ProjectileName = data.ProjectileName,
                WeaponName = data.WeaponName,
                Distance = data.Distance
            };
        }
    }

    public class WoundData : BaseKillData
    {
    }
    
    public class StatManager : SingletonComponent<StatManager>
    {
        public static Dictionary<ulong, WoundData> WoundData = new Dictionary<ulong, WoundData>();
        public static HashSet<Action<BasePlayer, PlayerKillData>> KillCallbacks = new HashSet<Action<BasePlayer, PlayerKillData>>();

        public static void RegisterKillCallback(Action<BasePlayer, PlayerKillData> callback)
        {
            KillCallbacks.Add(callback);
        }

        public static void UnregisterKillCallback(Action<BasePlayer, PlayerKillData> callback)
        {
            KillCallbacks.Remove(callback);
        }
        
        public static void ProcessWounded(BasePlayer victim, HitInfo info)
        {
            if (victim == null)
            {
                Debug.LogWarning($"Victim was null in ProcessWounded");
                return;
            }

            WoundData[victim.userID] = BaseKillData.FromHitInfo<WoundData>(victim, info); 
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

            PlayerKillData killdata;
            
            WoundData.TryGetValue(victim.userID, out var wounddata);

            if (info == null && wounddata == null)
            {
                Debug.LogWarning($"Victim {victim} had a null HitInfo and no WoundData in OnKilled?");
                return;
            }

            killdata = info == null ? PlayerKillData.FromWoundData(wounddata) : BaseKillData.FromHitInfo<PlayerKillData>(victim, info);

            if (wounddata != null)
            {
                killdata.AssistName = wounddata.AttackerName;
                killdata.AssistID = wounddata.AttackerID;
            }
            
            RustUtils.BroadcastCustom("PlayerKill", killdata);
            
            foreach (var callback in KillCallbacks)
            {
                try
                {
                    callback(victim, killdata);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Exception in KillCallback {nameof(callback)}: {e}");
                    Debug.LogException(e);
                }
            }
        }
    }
}
