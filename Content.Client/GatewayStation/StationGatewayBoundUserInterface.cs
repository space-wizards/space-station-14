using Robust.Client.UserInterface;

namespace Content.Client.GatewayStation;

public sealed class StationGatewayBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private StationGatewayWindow? _menu;

    public StationGatewayBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
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

        _menu = this.CreateWindow<StationGatewayWindow>();
        _menu.Set(stationName, gridUid);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        //switch (state)
        //{
        //    case GatewayStationState st:
        //}
    }
}
