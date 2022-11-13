using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mech.Components;

namespace Content.Shared.Mech.EntitySystems;

public abstract partial class SharedMechSystem
{
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;

    private void InitializeRelay()
    {

    }

    private void Relay(EntityUid uid, MechPilotComponent component, object args)
    {
        Logger.Debug("foo");
    }
}
