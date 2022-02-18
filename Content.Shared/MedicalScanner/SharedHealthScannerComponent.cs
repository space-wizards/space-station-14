using System;
using System.Collections.Generic;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.HealthScanner
{
    public abstract class SharedHealthScannerComponent : Component
    {
        [Serializable, NetSerializable]
        public class HealthComponentSyncRequestMessage : BoundUserInterfaceMessage
        {
        }


        [Serializable, NetSerializable]
        public class HealthComponentDamageMessage : BoundUserInterfaceMessage
        {
            public readonly string? TargetName;
            public readonly bool? IsAlive;
            public readonly string? TotalDamage;
            public readonly List<MobDamageGroup> DamageGroups;

            public HealthComponentDamageMessage(
                string? targetName,
                bool? isAlive,
                string? totalDamage,
                List<MobDamageGroup> damageGroups)
            {
                TargetName = targetName;
                IsAlive = isAlive;
                TotalDamage = totalDamage;
                DamageGroups = damageGroups;
            }
        }

        [Serializable, NetSerializable]
        public readonly struct MobDamageGroup
        {
            public readonly string? GroupName { get; }
            public readonly string? GroupTotalDamage { get; }
            public readonly Dictionary<string, string>? GroupedMinorDamages { get; }
            public MobDamageGroup(string? groupName, string? groupTotalDamage, Dictionary<string, string>? groupedMinorDamages)
            {
                GroupName = groupName;
                GroupTotalDamage = groupTotalDamage;
                GroupedMinorDamages = groupedMinorDamages;
            }
        }

        [Serializable, NetSerializable]
        public enum HealthScannerUiKey
        {
            Key
        }
    }
}
