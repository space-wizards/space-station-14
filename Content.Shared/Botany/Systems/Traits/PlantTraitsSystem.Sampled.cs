using Content.Shared.Botany.Components;
using Content.Shared.Botany.Events;
using Content.Shared.Popups;

namespace Content.Shared.Botany.Systems;

/// <summary>
/// The plant has been sampled, preventing it from being sampled again.
/// </summary>
[DataDefinition]
public sealed partial class TraitSampled : PlantTrait
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void OnPlantSampleAttempt(Entity<PlantTraitsComponent> ent, ref PlantSampleAttemptEvent args)
    {
        _popup.PopupPredictedCursor(Loc.GetString("plant-holder-component-already-sampled-message"), args.User);
        args.Cancel();
    }

    public override IEnumerable<string> GetTraitStateMarkup()
    {
        yield return Loc.GetString("mutation-plant-sampled");
    }
}
