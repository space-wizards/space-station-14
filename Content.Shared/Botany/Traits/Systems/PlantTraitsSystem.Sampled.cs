using Content.Shared.Botany.Events;
using Content.Shared.Botany.Traits.Components;
using Content.Shared.Popups;

namespace Content.Shared.Botany.Traits.Systems;

/// <inheritdoc cref="PlantTraitSampledComponent"/>
public sealed partial class PlantTraitSampledSystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popup = default!;

    [SubscribeLocalEvent]
    private void OnPlantSampleAttempt(Entity<PlantTraitSampledComponent> ent, ref PlantSampleAttemptEvent args)
    {
        _popup.PopupPredictedCursor(Loc.GetString("plant-sample-component-already-sampled-popup"), args.User);
        args.Cancel();
    }
}
