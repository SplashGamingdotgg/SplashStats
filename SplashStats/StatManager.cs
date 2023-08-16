using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Rust;
using UnityEngine;
using SplashUtilities;

namespace SplashStats
{
    public class BaseKillData
    {
        public string AttackerName;
        public ulong AttackerID;
        public string AttackerVehicle;
        public bool AttackerDriving;
        public string VictimName;
        public ulong VictimID;
        public bool VictimSleeping;
        public string VictimVehicle;
        public bool VictimDriving;
        public string BoneName;
        public int Distance;

        [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public HitArea BoneArea;

        public float ProjectileDistance;
        public string ProjectileName;
        public string WeaponName;

        [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public DamageType DamageType;
        
        // Stuff plugins or other Harmony mods can use, but shouldn't be serialized. 
        private HitInfo _hitInfo;
        private BasePlayer _victimPlayer;
        
        public void SetVictimPlayer(BasePlayer victimPlayer) => _victimPlayer = victimPlayer;
        public BasePlayer GetVictimPlayer() => _victimPlayer;
        public void SetHitInfo(HitInfo info) => _hitInfo = info;
        public HitInfo GetHitInfo() => _hitInfo;
        
        public bool IsSelfInflicted() => AttackerID == 0 && ProjectileName == null && WeaponName == null;
    
        public static T FromHitInfo<T>(BasePlayer victim, HitInfo info) where T : BaseKillData, new()
        {
            var data = new T()
            {
                _victimPlayer = victim,
                _hitInfo = info,
                BoneName = info.boneName,
                BoneArea = info.boneArea,
                ProjectileDistance = info.ProjectileDistance,
                ProjectileName = info.ProjectilePrefab?.name,
                WeaponName = info.WeaponPrefab?.ShortPrefabName,
                DamageType =  info.damageTypes.GetMajorityDamageType(),
                VictimSleeping = victim.IsSleeping()
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

                var vehicle = victim.GetMounted()?.VehicleParent();
                if (vehicle != null)
                {
                    data.VictimVehicle = vehicle.ShortPrefabName;
                    data.VictimDriving = victim == vehicle.GetDriver();
                }
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
                    var vehicle = info.InitiatorPlayer.GetMounted()?.VehicleParent();
                    if (vehicle != null)
                    {
                        data.AttackerVehicle = vehicle.ShortPrefabName;
                        data.AttackerDriving = info.InitiatorPlayer == vehicle.GetDriver();
                    }

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
            var pk = new PlayerKillData
            {
                AttackerName = data.AttackerName,
                AttackerID = data.AttackerID,
                VictimName = data.VictimName,
                VictimID = data.VictimID,
                VictimSleeping = data.VictimSleeping,
                VictimDriving = data.VictimDriving,
                VictimVehicle = data.VictimVehicle,
                AttackerDriving = data.AttackerDriving,
                AttackerVehicle = data.AttackerVehicle,
                BoneName = data.BoneName,
                BoneArea = data.BoneArea,
                ProjectileDistance = data.ProjectileDistance,
                ProjectileName = data.ProjectileName,
                WeaponName = data.WeaponName,
                Distance = data.Distance
            };
            pk.SetHitInfo(data.GetHitInfo());
            pk.SetVictimPlayer(data.GetVictimPlayer());

            return pk;
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
            Debug.Log($"Registering kill callback: {callback.Method.Name}");
            KillCallbacks.Add(callback);
        }

        public static void UnregisterKillCallback(Action<BasePlayer, PlayerKillData> callback)
        {
            Debug.Log($"Unregistered kill callback {callback.Method.Name}");
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

            WoundData.Remove(victim.userID);
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

            if (wounddata != null && info != null && info.damageTypes.GetMajorityDamageType() == DamageType.Suicide)
            {
                killdata = PlayerKillData.FromWoundData(wounddata);
            }
            else
            {
                killdata = info == null ? PlayerKillData.FromWoundData(wounddata) : BaseKillData.FromHitInfo<PlayerKillData>(victim, info);
            }
            
            if (wounddata != null)
            {
                killdata.AssistName = wounddata.AttackerName;
                killdata.AssistID = wounddata.AttackerID;

                WoundData.Remove(victim.userID);
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
