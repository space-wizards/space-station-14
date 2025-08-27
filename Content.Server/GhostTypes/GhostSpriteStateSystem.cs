using Content.Shared.Damage;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Robust.Shared.Random;

namespace Content.Server.GhostTypes;

public sealed class GhostSpriteStateSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    /// <summary>
    /// It goes trough an entity damage and assigns them a sprite according to the highest damage type/s
    /// </summary>
    public void SetGhostSprite(Entity<GhostSpriteStateComponent> ent, MindComponent mind)
    {
        if (!TryComp<AppearanceComponent>(ent, out var appearance))
            return;

        List<string> highestType;
        if (TryComp<DamageableComponent>(mind.CurrentEntity, out var damageComp))
        {
            highestType = _damageable.GetHighestDamageTypes(damageComp.DamagePerGroup, damageComp.Damage);
        }
        else if (mind.DamagePerGroup != null && mind.Damage != null)
        {
            highestType = _damageable.GetHighestDamageTypes(mind.DamagePerGroup, mind.Damage);
        }
        else
            return;

        if (highestType.Count == 0)
            return;

        highestType.Sort();  // sort if alphabetically

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
        _appearance.SetData(ent, GhostVisuals.Damage, ent.Comp.Prefix + spriteState.ToLower(), appearance);
    }
}
