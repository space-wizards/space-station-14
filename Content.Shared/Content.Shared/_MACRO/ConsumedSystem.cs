using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs;
using Content.Shared._MACRO.Species.Kodepiia.Consume.Components;
using System.Linq;

namespace Content.Shared._MACRO.Species.Kodepiia.Consume;

/// <summary>
/// This system handles entities that have been consumed by entites with the ConsumeActionComponent.
/// </summary>
public sealed partial class ConsumedSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void OnExamine(Entity<ConsumedComponent> ent, ref ExaminedEvent args)
    {
        var consumed = ent.Comp.ConsumedValue;
        var target = Identity.Entity(ent, EntityManager);

        // Filter thresholds by whatever equals or exceeds consume value.
        var validThresholds = ent.Comp.ExamineThresholds.Where(kvp => consumed >= kvp.Key);
        var keyValuePairs = validThresholds.ToList();
        if (!keyValuePairs.Any())
            return;

        // Get the highest valid tooltip and use it as examine text.
        var examineTooltip = keyValuePairs.MaxBy(kvp => kvp.Key).Value;
        args.PushMarkup(Loc.GetString(examineTooltip, ("target", target)));
    }

    [SubscribeLocalEvent]
    private void OnMobStateChange(Entity<ConsumedComponent> ent, ref MobStateChangedEvent args)
    {
        // If the entity is like, revived, it should no longer be considered "consumed"
        if (args.NewMobState == MobState.Alive)
            RemComp<ConsumedComponent>(ent);
    }
}
