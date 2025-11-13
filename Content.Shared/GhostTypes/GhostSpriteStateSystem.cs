using System.Linq;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Ghost;
using Content.Shared.Mind;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.GhostTypes;

public sealed class GhostSpriteStateSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly ProtoId<DamageTypePrototype> BluntProtoId = "Blunt";
    private readonly ProtoId<DamageTypePrototype> HeatProtoId = "Heat";
    private readonly ProtoId<DamageTypePrototype> PiercingProtoId = "Piercing";

    /// <summary>
    /// It goes through an entity damage and assigns them a sprite according to the highest damage type/s
    /// </summary>
    public void SetGhostSprite(Entity<GhostSpriteStateComponent?> ent, EntityUid mind)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;
        if (!TryComp<AppearanceComponent>(ent, out var appearance) || !TryComp<MindComponent>(mind, out var mindComp))
            return;

        var damageTypes = new Dictionary<ProtoId<DamageTypePrototype>, FixedPoint2>();

        if (TryComp<DamageableComponent>(mindComp.CurrentEntity, out var damageComp))
        {
            damageTypes = _damageable.GetDamages(damageComp.DamagePerGroup, damageComp.Damage);
        }
        else if (TryComp<LastBodyDamageComponent>(mind, out var storedDamage) && storedDamage.DamagePerGroup != null && storedDamage.Damage != null)
        {
            Dirty(mind, storedDamage);
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

        // TODO: Replace with RandomPredicted once the engine PR is merged
        var seed = SharedRandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, GetNetEntity(ent).Id);
        var rand = new System.Random(seed);

        highestTypes.Sort();
        if (highestTypes.Count == 3 && highestTypes[0] == BluntProtoId && highestTypes[1] == HeatProtoId && highestTypes[2] == PiercingProtoId) // Specific case for explosions (not an ideal way of doing it)
        {
            spriteState = "Explosion" + rand.Next(0, 3);
        }
        else
        {
            spriteState = highestTypes[rand.Next(0, highestTypes.Count)]; // Chooses a random damage type from the list
            if (ent.Comp.DamageMap.TryGetValue(spriteState, out var spriteAmount))  // Chooses a random sprite state
            {
                spriteState += rand.Next(0, spriteAmount - 1);
            }
        }
        _appearance.SetData(ent, GhostComponent.GhostVisuals.Damage, ent.Comp.Prefix + spriteState, appearance);
    }
}
