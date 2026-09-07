using Content.Shared.Botany.Events;
using Content.Shared.Botany.Traits.Components;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.Botany.Traits.Systems;

/// <inheritdoc cref="PlantTraitScreamComponent"/>
public sealed partial class PlantTraitScreamSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;

    [SubscribeLocalEvent]
    private void OnPlantHarvested(Entity<PlantTraitScreamComponent> ent, ref PlantHarvestedEvent args)
    {
        _audio.PlayPredicted(ent.Comp.ScreamSound, ent, args.User);
    }
}
