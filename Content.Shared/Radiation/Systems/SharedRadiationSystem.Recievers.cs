using Content.Shared.Radiation.Components;
using Content.Shared.Radiation.Events;

namespace Content.Shared.Radiation.Systems;

public partial class SharedRadiationSystem
{
    private void UpdateReceivers()
    {
        foreach (var receiver in EntityQuery<RadiationReceiverComponent>())
        {
            var mapCoordinates = Transform(receiver.Owner).MapPosition;
            if (!_mapManager.TryFindGridAt(mapCoordinates, out var candidateGrid) ||
                !candidateGrid.TryGetTileRef(candidateGrid.WorldToTile(mapCoordinates.Position), out var tileRef))
            {
                return;
            }

            var gridUid = tileRef.GridUid;
            var pos = tileRef.GridIndices;
            if (!_radiationMap.TryGetValue(gridUid, out var map) ||
                !map.TryGetValue(pos, out var rads))
                return;

            var ev = new OnIrradiatedEvent(RadiationCooldown, rads);
            RaiseLocalEvent(receiver.Owner, ev);
        }
    }
}
