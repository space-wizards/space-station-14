using Robust.Client.UserInterface;

namespace Content.Client.Pinpointer.UI;

public sealed class UntrackedStationMapBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private StationMapWindow? _window;

    public UntrackedStationMapBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        EntityUid? gridUid = null;

        // TODO: What this just looks like it's been copy-pasted wholesale from StationMapBoundUserInterface?
        if (EntMan.TryGetComponent<TransformComponent>(Owner, out var xform))
        {
            gridUid = xform.GridUid;
        }

        _window = this.CreateWindow<StationMapWindow>();
        _window.Set(gridUid, Owner);
    }
}
