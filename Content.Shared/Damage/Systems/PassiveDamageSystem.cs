using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Mobs.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.FixedPoint;
using Robust.Shared.Timing;
using Robust.Shared.Toolshed.TypeParsers;
using System.Xml;
using System.Collections;
using Robust.Shared.Toolshed.Commands.Math;
using YamlDotNet.Core.Tokens;
using Content.Shared.EntityEffects.EffectConditions;
using System.Diagnostics;

namespace Content.Shared.Damage;

public sealed class PassiveDamageSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PassiveDamageComponent, MapInitEvent>(OnPendingMapInit);
    }

    private void OnPendingMapInit(EntityUid uid, PassiveDamageComponent component, MapInitEvent args)
    {
        component.NextDamage = _timing.CurTime + TimeSpan.FromSeconds(1f);
    }

    // Every tick, attempt to damage entities
    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var curTime = _timing.CurTime;

        // Go through every entity with the component
        var query = EntityQueryEnumerator<PassiveDamageComponent, DamageableComponent, MobStateComponent>();
        while (query.MoveNext(out var uid, out var comp, out var damage, out var mobState))
        {
            DamageSpecifier specifcDamage = new();
            // Make sure they're up for a damage tick
            if (comp.NextDamage > curTime)
                continue;

            // Make sure they can take damage
            if (comp.DamageCap != 0 && damage.TotalDamage >= comp.DamageCap)
                continue;

            //See if they can take the entire damage specifier.
            if (damage.TotalDamage <= comp.DamageCap)
                specifcDamage = new();
            else if (comp.SeperateDamageCap.Empty == false)
            //Check if any specific damage types are eligible to be healed, and if so construct a damagespecifier out of just those.
            {
                //Set the damage to what types have seperate caps, along with their modifiers.
                specifcDamage = new(comp.SeperateDamageCap);
                //Add the current damage in these types to the modifers in their seperate damagecaps.
                specifcDamage.ExclusiveAdd(damage.Damage);
                //Remove any types that are greater than the damage cap.
                specifcDamage.TrimMax(comp.DamageCap);
                //Set all values of remaining types to 0
                specifcDamage.Clamp(0, 0);
                //Add any healing to the corresponding value.

            }
            // Set the next time they can take damage
            comp.NextDamage = curTime + TimeSpan.FromSeconds(1f);

            // Damage them
            foreach (var allowedState in comp.AllowedStates)
            {
                if (allowedState == mobState.CurrentState)
                    _damageable.TryChangeDamage(uid, specifcDamage, true, false, damage);
            }
        }
    }
}
