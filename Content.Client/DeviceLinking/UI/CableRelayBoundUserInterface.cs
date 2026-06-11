using Content.Shared.DeviceLinking;
using Content.Shared.Power;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.DeviceLinking.UI;

[UsedImplicitly]
public sealed class CableRelayBoundUserInterface : BoundUserInterface
{
    private CableRelayWindow? _window;

    public CableRelayBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<CableRelayWindow>();
        _window.OnTogglePressed += () => SendPredictedMessage(new CableRelayToggleMessage());
        _window.OnCableTypeToggled += (type, enabled) => SendPredictedMessage(new CableRelaySetCableTypeMessage(type, enabled));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is not CableRelayBoundUserInterfaceState castState || _window == null)
            return;

        _window.UpdateState(castState.Powered, castState.Severed, castState.AffectedTypes);
    }
}
