using Content.Shared.Gateway;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Shared.Timing;

namespace Content.Client.Gateway.UI;

[UsedImplicitly]
public sealed partial class GatewayBoundUserInterface : BoundUserInterface
{
    private static readonly EntityTimerId UnlockTimer = new("unlock");
    private static readonly EntityTimerId CooldownTimer = new("cooldown");
    private static readonly EntityTimerId RefreshTimer = new("refresh");

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

        SetDeadline(UnlockTimer, current.NextUnlock);
        SetDeadline(CooldownTimer, current.NextReady);
        var (unlock, cooldown) = GetRemaining();
        _window?.UpdateState(current, unlock, cooldown);

        if (unlock > TimeSpan.Zero || cooldown > TimeSpan.Zero)
            SetTimer(RefreshTimer, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        else
            CancelTimer(RefreshTimer);
    }

    protected override void OnTimer(EntityTimerEvent timer)
    {
        if (timer.Id != UnlockTimer && timer.Id != CooldownTimer && timer.Id != RefreshTimer)
            return;

        var (unlock, cooldown) = GetRemaining();
        _window?.Refresh(unlock, cooldown);
        if (unlock <= TimeSpan.Zero && cooldown <= TimeSpan.Zero)
            CancelTimer(RefreshTimer);
    }

    private void SetDeadline(EntityTimerId id, TimeSpan deadline)
    {
        if (deadline == TimeSpan.Zero)
            CancelTimer(id);
        else
            SetTimerAt(id, deadline);
    }

    private (TimeSpan Unlock, TimeSpan Cooldown) GetRemaining()
    {
        var unlock = TryGetTimer(UnlockTimer, out var unlockTimer) ? unlockTimer.Remaining : TimeSpan.Zero;
        var cooldown = TryGetTimer(CooldownTimer, out var cooldownTimer) ? cooldownTimer.Remaining : TimeSpan.Zero;
        return (unlock, cooldown);
    }
}
