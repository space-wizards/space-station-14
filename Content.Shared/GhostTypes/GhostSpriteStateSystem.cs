using Content.Shared.Damage;
using Content.Shared.Ghost;
using Content.Shared.Mind;
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

        List<string> highestType;
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
        if (highestType is ["Blunt", "Heat", "Piercing"])  // special case for explosions
        {
            spriteState = "explosion" + _random.Next(1, 4);  // Chooses between 3 possible sprites
        }
        else
        {
            if (highestType[0] == "Blunt"
                || highestType[0] == "Slash"
                || highestType[0] == "Piercing")
            {
                spriteState = highestType[0] + _random.Next(1, 4); // Chooses between 3 possible sprites
            }
            else
            {
                spriteState = highestType[_random.Next(0, highestType.Count)]; // Uses the 1 possible sprite
            }
        }
        _appearance.SetData(ent, GhostComponent.GhostVisuals.Damage, ent.Comp.Prefix + spriteState.ToLower(), appearance);
    }
}
