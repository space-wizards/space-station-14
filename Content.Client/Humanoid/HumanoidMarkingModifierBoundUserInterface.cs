using Content.Shared.Humanoid;
using Robust.Client.UserInterface;

namespace Content.Client.Humanoid;

// Marking BUI.
// Do not use this in any non-privileged instance. This just replaces an entire marking set
// with the set sent over.

public sealed class HumanoidMarkingModifierBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private HumanoidMarkingModifierWindow? _window;

    private readonly MarkingsViewModel _markingsModel = new();

    public HumanoidMarkingModifierBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindowCenteredLeft<HumanoidMarkingModifierWindow>();
        _window.MarkingPickerWidget.SetModel(_markingsModel);
        _window.RespectLimits.OnPressed += args => _markingsModel.EnforceLimits = args.Button.Pressed;
        _window.RespectGroupSex.OnPressed += args => _markingsModel.EnforceGroupAndSexRestrictions = args.Button.Pressed;

        _markingsModel.MarkingsChanged += (_, _) => SendMarkingSet();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_window == null || state is not HumanoidMarkingModifierState cast)
            return;

        _markingsModel.OrganData = cast.OrganData;
        _markingsModel.OrganProfileData = cast.OrganProfileData;
        _markingsModel.Markings = cast.Markings;
    }

    private void SendMarkingSet()
    {
        SendMessage(new HumanoidMarkingModifierMarkingSetMessage(_markingsModel.Markings));
    }
}


