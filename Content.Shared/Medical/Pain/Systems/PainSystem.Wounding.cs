
using Content.Shared.Medical.Wounds.Components;
using Content.Shared.Medical.Wounds.Systems;

namespace Content.Shared.Medical.Pain.Systems;

public sealed partial class PainSystem
{
    private void OnWoundAdded(EntityUid target, WoundableComponent component, ref WoundAddedEvent args)
    {


    }

    private void OnWoundRemoved(EntityUid target, WoundableComponent component, ref WoundRemovedEvent args)
    {

    }
}
