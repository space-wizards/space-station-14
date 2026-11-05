using System.Linq;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Ghost.Components;
using Content.Shared.Mind;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.GhostTypes;

public sealed partial class GhostSpriteStateSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private IGameTiming _timing = default!;

    /// <summary>
    /// It goes through an entity damage and assigns them a sprite according to the highest damage type/s
    /// </summary>
    public void SetGhostSprite(Entity<GhostSpriteStateComponent?> ent, EntityUid mind)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (!TryComp<AppearanceComponent>(ent, out var appearance) || !HasComp<MindComponent>(mind))
            return;

        if (!TryComp<LastBodyDamageComponent>(mind, out var storedDamage)
            || storedDamage.Damage.DamageDict.Count == 0)
            return;

        // We only need the highest value
        var highestType = storedDamage.Damage.DamageDict.OrderByDescending(x => x.Value).First().Key;
        var specialCase = storedDamage.SpecialCauseOfDeath;

        var rand = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent));

        ProtoId<DamageTypePrototype>? spriteState = null;

        if (specialCase != null)  // Possible special cases like death by an explosion
        {
            var prototype = ProtoMan.Index(specialCase);
            spriteState = specialCase + rand.Next(prototype.NumOfStates);
        }
        else if (ent.Comp.DamageMap.TryGetValue(highestType, out var spriteAmount))
        {
            spriteState = highestType + rand.Next(spriteAmount);
        }

        if (spriteState != null)
            _appearance.SetData(ent, GhostVisuals.Damage, ent.Comp.Prefix + spriteState, appearance);
    }
}
