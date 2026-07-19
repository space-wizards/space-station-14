using Content.Shared.Gateway;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Shared.Timing;

namespace Content.Client.Gateway.UI;

[UsedImplicitly]
public sealed partial class GatewayBoundUserInterface : BoundUserInterface
{
    private static readonly EntityTimerId RefreshTimer = new("refresh");

    [Dependency] private IGameTiming _timing = default!;

    private GatewayWindow? _window;

    public GatewayBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<GatewayWindow>();
        _window.SetEntity(EntMan.GetNetEntity(Owner));

        _window.OpenPortal += destination =>
        {
            SendMessage(new GatewayOpenPortalMessage(destination));
        };
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not GatewayBoundUserInterfaceState current)
            return;

        _window?.UpdateState(current, _timing.CurTime);
        SetTimer(RefreshTimer, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
    }

    protected override void OnTimer(EntityTimerEvent timer)
    {
        if (timer.Id == RefreshTimer)
            _window?.Refresh(timer.FiredAt);
    }
}
