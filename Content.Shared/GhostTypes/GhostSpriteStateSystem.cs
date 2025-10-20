using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Ghost;
using Content.Shared.Mind;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.GhostTypes;

public sealed class GhostSpriteStateSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    /// <summary>
    /// It goes through an entity damage and assigns them a sprite according to the highest damage type/s
    /// </summary>
    public void SetGhostSprite(Entity<GhostSpriteStateComponent> ent, EntityUid mind)
    {
        if (!TryComp<AppearanceComponent>(ent, out var appearance) || !TryComp<MindComponent>(mind, out var mindComp))
            return;

        List<ProtoId<DamageTypePrototype>> highestType;
        if (TryComp<DamageableComponent>(mindComp.CurrentEntity, out var damageComp))
        {
            highestType = _damageable.GetHighestDamageTypes(damageComp.DamagePerGroup, damageComp.Damage);
        }
        else if (TryComp<LastBodyDamageComponent>(mind, out var storedDamage) && storedDamage.DamagePerGroup != null && storedDamage.Damage != null)
        {
            highestType = _damageable.GetHighestDamageTypes(storedDamage.DamagePerGroup, storedDamage.Damage);
        }
        else
            return;

        if (highestType.Count == 0)
            return;

        highestType.Sort();

        string spriteState;
        if (highestType[0] == "Blunt" && highestType[1] == "Heat" && highestType[2] == "Piercing")  // Specific case for explosions
        {
            spriteState = "explosion" +_random.Next(1, 4);
        }
        else
        {
            spriteState = highestType[_random.Next(0, highestType.Count)]; // Chooses a random damage type from the list
            if (ent.Comp.DamageMap.TryGetValue(highestType[_random.Next(0, highestType.Count)], out var spriteAmount) && spriteAmount > 1)  // Chooses a random sprite state if needed.
            {
                spriteState += _random.Next(1, spriteAmount + 1);
            }
        }

        _appearance.SetData(ent, GhostComponent.GhostVisuals.Damage, ent.Comp.Prefix + spriteState.ToLower(), appearance);
    }
}
