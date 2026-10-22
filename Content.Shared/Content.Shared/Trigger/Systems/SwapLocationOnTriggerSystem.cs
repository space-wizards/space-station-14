using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.Network;

namespace Content.Shared.Trigger.Systems;

public sealed partial class SwapLocationOnTriggerSystem : EntitySystem
{
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private INetManager _net = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SwapLocationOnTriggerComponent, TriggerEvent>(OnTrigger);
    }

    private void OnTrigger(Entity<SwapLocationOnTriggerComponent> ent, ref TriggerEvent args)
    {
        if (args.Key != null && !ent.Comp.KeysIn.Contains(args.Key))
            return;

        if (args.User == null)
            return;

        // SwapPositions mispredicts at the moment.
        // TODO: Fix this and remove the IsServer check.
        if (_net.IsServer)
            _transform.SwapPositions(ent.Owner, args.User.Value);
        args.Handled = true;
    }
}
