using Content.Shared.APC;
using Robust.Client.GameObjects;

namespace Content.Client.Power.APC;

/// <inheritdoc/>
public sealed partial class ClientApcSystem : ApcSystem
{
    [Dependency] private UserInterfaceSystem _ui = default!;

    [SubscribeLocalEvent]
    private void OnApcState(Entity<ApcComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateUI(ent);
    }

    protected override void UpdateUI(Entity<ApcComponent> ent)
    {
        if (_ui.TryGetOpenUi<ApcBoundUserInterface>(ent.Owner, ApcUiKey.Key, out var bui))
            bui.UpdateBreaker(ent.Comp.MainBreakerEnabled);
    }
}
