using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.Network;

namespace Content.Shared.Trigger.Systems;

public sealed partial class SwapLocationOnTriggerSystem : XOnTriggerSystem<SwapLocationOnTriggerComponent>
{
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private INetManager _net = default!;

    protected override void OnTrigger(Entity<SwapLocationOnTriggerComponent> ent, EntityUid _, ref TriggerEvent args)
    {
        if (args.User == null)
            return;

        // SwapPositions mispredicts at the moment.
        // TODO: Fix this and remove the IsServer check.
        if (_net.IsServer)
            _transform.SwapPositions(ent.Owner, args.User.Value);
        args.Handled = true;
    }
}
