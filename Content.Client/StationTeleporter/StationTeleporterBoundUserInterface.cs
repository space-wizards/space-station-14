using Content.Shared.StationTeleporter;
using Robust.Client.UserInterface;

namespace Content.Client.StationTeleporter;

public sealed class StationTeleporterBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private StationTeleporterWindow? _menu;

    public StationTeleporterBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        EntityUid? gridUid = null;
        var stationName = string.Empty;

        if (EntMan.TryGetComponent<TransformComponent>(Owner, out var xform))
        {
            gridUid = xform.GridUid;

            if (EntMan.TryGetComponent<MetaDataComponent>(gridUid, out var metaData))
            {
                stationName = metaData.EntityName;
            }
        }

        _menu = this.CreateWindow<StationTeleporterWindow>();
        _menu.Set(this, stationName, gridUid);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        switch (state)
        {
            case StationTeleporterState st:
                EntMan.TryGetComponent<TransformComponent>(Owner, out var xform);
                _menu?.ShowTeleporters(st, Owner, xform?.Coordinates);
                break;
        }
    }

    public void SendTeleporterLinkChangeMessage(NetEntity? teleporter)
    {
        SendMessage(new StationTeleporterClickMessage(teleporter));
    }
}
