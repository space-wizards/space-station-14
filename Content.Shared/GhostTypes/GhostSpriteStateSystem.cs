using System.Linq;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
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

        var damageTypes = new Dictionary<ProtoId<DamageTypePrototype>, FixedPoint2>();

        if (TryComp<DamageableComponent>(mindComp.CurrentEntity, out var damageComp))
        {
            damageTypes = _damageable.GetDamages(damageComp.DamagePerGroup, damageComp.Damage);
        }
        else if (TryComp<LastBodyDamageComponent>(mind, out var storedDamage) && storedDamage.DamagePerGroup != null && storedDamage.Damage != null)
        {
            damageTypes = _damageable.GetDamages(storedDamage.DamagePerGroup, storedDamage.Damage);
        }
        else
            return;

        if (damageTypes.Count == 0)
            return;

        var spriteState = new ProtoId<DamageTypePrototype>();
        var sortedDict = damageTypes.OrderBy(x => x.Value).ToDictionary();
        List<ProtoId<DamageTypePrototype>> highestTypes = new();

        for (var i = sortedDict.Count - 1; i >= 0; i--) // Go through the dictionary values and save the ProtoId's of the highest value
        {
            if (sortedDict.ElementAt(i).Value == sortedDict.ElementAt(sortedDict.Count - 1).Value)
            {
                highestTypes.Add(sortedDict.ElementAt(i).Key);
            }
        }

        highestTypes.Sort();
        if (highestTypes.Count == 3 && highestTypes[0] == "Blunt" && highestTypes[1] == "Heat" && highestTypes[2] == "Piercing") // Specific case for explosions (not an ideal way of doing it)
        {
            spriteState = "Explosion" + _random.Next(1, 4);
        }
        else
        {
            spriteState = highestTypes[_random.Next(0, highestTypes.Count)]; // Chooses a random damage type from the list
            if (ent.Comp.DamageMap.TryGetValue(spriteState, out var spriteAmount) && spriteAmount > 1)  // Chooses a random sprite state if needed.
            {
                spriteState += _random.Next(1, spriteAmount + 1);
            }
        }
        _appearance.SetData(ent, GhostComponent.GhostVisuals.Damage, ent.Comp.Prefix + spriteState, appearance);
    }
}
