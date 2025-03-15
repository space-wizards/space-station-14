using Content.Server._DV.DeviceLinking.Components;
using Content.Server.DeviceLinking.Systems;
using Content.Shared.Hands;
using Content.Shared.Item.ItemToggle;

namespace Content.Server._DV.DeviceLinking.Systems;

public sealed class DeadMansSignallerSystem : EntitySystem
{
    [Dependency] private readonly DeviceLinkSystem _link = default!;
    [Dependency] private readonly ItemToggleSystem _toggle = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DeadMansSignallerComponent, GotUnequippedHandEvent>(DeadMans);
    }

    private void DeadMans(Entity<DeadMansSignallerComponent> ent, ref GotUnequippedHandEvent args)
    {
        if (_toggle.IsActivated(ent.Owner))
        {
            _link.InvokePort(ent.Owner, ent.Comp.Port);
        }
    }
}
