using Content.Shared.Botany.Events;
using Content.Shared.Botany.Traits.Components;
using Content.Shared.Popups;

namespace Content.Shared.Botany.Traits.Systems;

/// <inheritdoc cref="PlantTraitSampledComponent"/>
public sealed partial class PlantTraitSampledSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PlantTraitSampledComponent, PlantSampleAttemptEvent>(OnPlantSampleAttempt);
    }

    private void OnPlantSampleAttempt(Entity<PlantTraitSampledComponent> ent, ref PlantSampleAttemptEvent args)
    {
        _popup.PopupPredictedCursor(Loc.GetString("plant-holder-component-already-sampled-message"), args.User);
        args.Cancel();
    }
}
