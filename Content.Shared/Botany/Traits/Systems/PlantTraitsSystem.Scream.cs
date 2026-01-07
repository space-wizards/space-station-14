using Content.Shared.Botany.Events;
using Content.Shared.Botany.Traits.Components;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.Botany.Traits.Systems;

/// <summary>
/// Plays a screams when the plant is harvested.
/// </summary>
public sealed partial class PlantTraitScreamSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PlantTraitScreamComponent, AfterDoHarvestEvent>(OnAfterDoHarvest);
    }

    private void OnAfterDoHarvest(Entity<PlantTraitScreamComponent> ent, ref AfterDoHarvestEvent args)
    {
        _audio.PlayPredicted(ent.Comp.ScreamSound, ent, args.User);
    }
}
