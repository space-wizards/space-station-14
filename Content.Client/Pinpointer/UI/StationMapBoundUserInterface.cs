using Content.Shared.Pinpointer;
using Robust.Client.UserInterface;

namespace Content.Client.Pinpointer.UI;

public sealed class StationMapBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private StationMapWindow? _window;

    public StationMapBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        EntityUid? gridUid = null;

        if (EntMan.TryGetComponent<TransformComponent>(Owner, out var xform))
        {
            gridUid = xform.GridUid;
        }

        _window = this.CreateWindow<StationMapWindow>();
        _window.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;

        string stationName = string.Empty;
        if(EntMan.TryGetComponent<MetaDataComponent>(gridUid, out var gridMetaData))
        {
            stationName = gridMetaData.EntityName;
        }
        
        if (EntMan.TryGetComponent<StationMapComponent>(Owner, out var comp) && comp.ShowLocation)
            _window.Set(stationName, gridUid, Owner);
        else
            _window.Set(stationName, gridUid, null);
    }
}
