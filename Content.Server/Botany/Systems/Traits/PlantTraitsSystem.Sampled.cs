using Content.Server.Botany.Components;
using Content.Server.Botany.Events;
using Content.Server.Popups;


namespace Content.Server.Botany.Systems;

/// <summary>
/// The plant has been sampled, preventing it from being sampled again.
/// </summary>
[DataDefinition]
public sealed partial class TraitSampled : PlantTrait
{
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void OnPlantSampleAttempt(Entity<PlantTraitsComponent> ent, ref PlantSampleAttemptEvent args)
    {
        _popup.PopupCursor(Loc.GetString("plant-holder-component-already-sampled-message"), args.User);
        args.Cancel();
    }

    public override IEnumerable<string> GetPlantStateMarkup()
    {
        yield return Loc.GetString("mutation-plant-sampled");
    }
}
