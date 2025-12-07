using System.Linq;
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
        var specialCase = "";

        if (!TryComp<LastBodyDamageComponent>(mind, out var storedDamage))
          return;

        if (storedDamage.DamagePerGroup != null && storedDamage.Damage != null)
        {
            damageTypes = _damageable.GetDamages(storedDamage.DamagePerGroup, storedDamage.Damage);
        }
        specialCase = storedDamage.SpecialCauseOfDeath;
        Dirty(mind, storedDamage);

        var damageTypesSorted = damageTypes.OrderByDescending(x => x.Value).ToDictionary();
        if (damageTypesSorted.Count == 0)
            return;

        var highestType = damageTypesSorted.First().Key; // We only need 1 of the values

        // TODO: Replace with RandomPredicted once the engine PR is merged
        var seed = SharedRandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, GetNetEntity(ent).Id);
        var rand = new System.Random(seed);

        ProtoId<DamageTypePrototype>? spriteState = null;

        if (specialCase != "")  // Possible special cases like death by an explosion
        {
            spriteState = specialCase + rand.Next(0, 3);
        }
        else
        {
            if (ent.Comp.DamageMap.TryGetValue(highestType, out var spriteAmount))  // Chooses a random sprite state if needed
                spriteState = highestType + rand.Next(0, spriteAmount - 1);
        }

        if (spriteState != null)
            _appearance.SetData(ent, GhostVisuals.Damage, ent.Comp.Prefix + spriteState, appearance);
    }
}
