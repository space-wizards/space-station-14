// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.DeadSpace.Autopsy;
using Content.Shared.Mobs;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.DeadSpace.Autopsy;

public sealed class HumanoidDamageSequenceSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<HumanoidDamageSequenceComponent, DamageChangedEvent>(OnDamageChange);
        SubscribeLocalEvent<HumanoidDamageSequenceComponent, MobStateChangedEvent>(OnMobState);
    }

    private void OnDamageChange(EntityUid uid, HumanoidDamageSequenceComponent component, DamageChangedEvent args)
    {
        if (args.DamageDelta == null || args.DamageDelta.DamageDict == null)
            return;

        if (args.DamageIncreased)
        {
            foreach (var damageGroups in args.Damageable.DamagePerGroup)
            {
                var group = _prototype.Index<DamageGroupPrototype>(damageGroups.Key);

                foreach (var damage in args.DamageDelta.DamageDict)
                {
                    if (group.DamageTypes.Contains(damage.Key))
                    {
                        DamageEntry damageEntry = new DamageEntry(_gameTiming.CurTime, group.ID, damage.Key, damage.Value);
                        component.DamageSequence.Add(damageEntry);
                    }
                }
            }

        }
        else if (!args.DamageIncreased)
        {
            if (args.Damageable.TotalDamage == 0)
            {
                component.DamageSequence.Clear();
            }
        }

        foreach (var group in args.Damageable.DamagePerGroup)
        {
            if (group.Value == 0)
            {
                foreach (var damageEntry in component.DamageSequence)
                {
                    if (group.Key == damageEntry.DamageGroup)
                    {
                        component.DamageSequence.Remove(damageEntry);
                    }
                }
            }
        }

        Dirty(uid, component);
    }

    private void OnMobState(EntityUid uid, HumanoidDamageSequenceComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
        {
            component.TimeOfDeath = _gameTiming.CurTime;
        }
    }
}
