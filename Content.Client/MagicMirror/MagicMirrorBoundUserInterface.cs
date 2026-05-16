using Content.Client.Humanoid;
using Content.Shared.MagicMirror;
using Robust.Client.UserInterface;

namespace Content.Client.MagicMirror;

public sealed class MagicMirrorBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private MagicMirrorWindow? _window;

    private readonly MarkingsViewModel _markingsModel = new();

    public MagicMirrorBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<MagicMirrorWindow>();

        _window.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;

        _window.MarkingsPicker.SetModel(_markingsModel);

        _markingsModel.MarkingsChanged += (_, _) =>
        {
            SendMessage(new MagicMirrorSelectMessage(_markingsModel.Markings));
        };
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not MagicMirrorUiState data)
            return;

        _markingsModel.OrganData = data.OrganMarkingData;
        _markingsModel.OrganProfileData = data.OrganProfileData;
        _markingsModel.Markings = data.AppliedMarkings;
    }
}

