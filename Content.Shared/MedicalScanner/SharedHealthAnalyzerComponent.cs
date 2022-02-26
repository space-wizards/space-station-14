using Robust.Shared.Serialization;

namespace Content.Shared.HealthAnalyzer
{
    public abstract class SharedHealthAnalyzerComponent : Component
    {
        [Serializable, NetSerializable]
        public class HealthAnalyzerBoundUserInterfaceState : BoundUserInterfaceState
        {
            public readonly string? TargetName;
            public readonly bool? IsAlive;
            public readonly string? TotalDamage;
            public readonly List<MobDamageGroup> DamageGroups;

            public HealthAnalyzerBoundUserInterfaceState(
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
        public enum HealthAnalyzerUiKey
        {
            Key
        }
    }
}
