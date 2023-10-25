using Content.Server.GameTicking.Rules.Components;
using Content.Server.Station.Systems;
using Content.Server.StationEvents.Metric.Components;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Roles;
using Content.Shared.Tag;
using Content.Shared.Zombies;

namespace Content.Server.StationEvents.Metric;

/// <summary>
///   Measures the strength of friendies and hostiles. Also calculates related health / death stats.
///
///   I've used 10 points per entity because later we might somehow estimate combat strength
///   as a multiplier. We could for instance detect damage delt / recieved and look also at
///   entity hitpoints & resistances as an analogue for danger.
///
///   Writes the following
///   Friend : -10 per each friendly entity on the station (negative is GOOD in chaos)
///   Hostile : about 10 points per hostile (those with antag roles) - varies per constants
///   Combat: friendlies + hostiles (to represent the balance of power)
///   Death: 20 per dead body,
///   Medical: 10 for crit + 0.05 * damage (so 5 for 100 damage),
/// </summary>
public sealed class CombatMetricSystem : ChaosMetricSystem<CombatMetricComponent>
{
    [Dependency] private readonly SharedRoleSystem _roles = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    public FixedPoint2 InventoryPower(EntityUid uid, CombatMetricComponent component)
    {
        // Iterate through items to determine how powerful the entity is
        // Having a good range of offensive items in your inventory makes you more dangerous
        var threat = FixedPoint2.Zero;

        var tagsQ = GetEntityQuery<TagComponent>();
        var allTags = new HashSet<string>();

        foreach (var item in _inventory.GetHandOrInventoryEntities(uid))
        {
            if (tagsQ.TryGetComponent(uid, out var tags))
            {
                allTags.UnionWith(tags.Tags);
            }
        }

        foreach (var key in allTags)
        {
            threat += component.itemThreat.GetValueOrDefault(key);
        }

        if (threat > component.maxItemThreat)
            return component.maxItemThreat;

        return threat;
    }

    public override ChaosMetrics CalculateChaos(EntityUid metric_uid, CombatMetricComponent combatMetric,
        CalculateChaosEvent args)
    {
        // Add up the pain of all the puddles
        var query = EntityQueryEnumerator<MindContainerComponent, MobStateComponent, DamageableComponent, TransformComponent>();
        var hostiles = FixedPoint2.Zero;
        var friendlies = FixedPoint2.Zero;

        var medical = FixedPoint2.Zero;
        var death = FixedPoint2.Zero;

        var powerQ = GetEntityQuery<CombatPowerComponent>();

        // var humanoidQ = GetEntityQuery<HumanoidAppearanceComponent>();
        var stationGrids = _stationSystem.GetAllStationGrids();

        while (query.MoveNext(out var uid, out var mind, out var mobState, out var damage, out var transform))
        {
            // Don't count anything that is mindless
            if (mind.Mind == null)
                continue;

            // Only count threats currently on station, which avoids salvage threats getting counted for instance.
            // Note this means for instance Nukies on nukie planet don't count, so the threat will spike when they arrive.
            if (transform.GridUid == null || !stationGrids.Contains(transform.GridUid.Value))
                // TODO: Check for NPCs here, they still count.
                continue;

            // Read per-entity scaling factor (for instance space dragon has much higher threat)
            powerQ.TryGetComponent(uid, out var power);
            var threatMultiple = power?.Threat ?? 1.0f;

            var entityThreat = FixedPoint2.Zero;

            var antag = _roles.MindIsAntagonist(mind.Mind);
            if (antag)
            {
                if (mobState.CurrentState != MobState.Alive)
                    continue;
            }
            else
            {
                // This is a friendly
                if (mobState.CurrentState == MobState.Dead)
                {
                    death += combatMetric.DeadScore;
                    continue;
                }
                else
                {
                    medical += damage.Damage.Total * combatMetric.MedicalMultiplier;
                    if (mobState.CurrentState == MobState.Critical)
                    {
                        medical += combatMetric.CritScore;
                        continue;
                    }
                }
            }

            // Iterate through items to determine how powerful the entity is
            entityThreat += InventoryPower(uid, combatMetric);
            if (antag)
                hostiles += (entityThreat + combatMetric.HostileScore) * threatMultiple;
            else
                friendlies += (entityThreat + combatMetric.FriendlyScore) * threatMultiple;
        }

        var chaos = new ChaosMetrics(new Dictionary<ChaosMetric, FixedPoint2>()
        {
            {ChaosMetric.Friend, -friendlies}, // Friendlies are good, so make a negative chaos score
            {ChaosMetric.Hostile, hostiles},

            {ChaosMetric.Death, death},
            {ChaosMetric.Medical, medical},
        });
        return chaos;
    }
}
