using Content.Shared.Botany.Events;
using Content.Shared.Botany.Traits.Components;
using Content.Shared.Botany.Systems;

namespace Content.Shared.Botany.Traits.Systems;

/// <summary>
/// Base system for managing plant traits.
/// </summary>
public sealed partial class PlantTraitsSystem : EntitySystem
{
    [Dependency] private MutationSystem _mutation = default!;

    [SubscribeLocalEvent]
    private void OnCrossPollinate(Entity<PlantTraitsComponent> ent, ref PlantCrossPollinateEvent args)
    {
        _mutation.CrossTrait(ent.Owner, args.PollenData);
    }
}
