using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Map;
using Content.Client.Pinpointer.UI;
using Content.Client.Resources;
using Content.Shared.SurveillanceCamera.Components;

namespace Content.Client.SurveillanceCamera.UI;

public sealed class SurveillanceCameraNavMapControl : NavMapControl
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;

    private static readonly Color CameraActiveColor = Color.FromHex("#FF00FF");
    private static readonly Color CameraInactiveColor = Color.FromHex("#a09f9fff");
    private static readonly Color CameraSelectedColor = Color.FromHex("#fbff19ff");
    private static readonly Color CameraInvalidColor = Color.FromHex("#fa1f1fff");

    private readonly Texture _cameraTexture;
    private string _activeCameraAddress = string.Empty;
    private HashSet<string> _availableSubnets = new();
    private (Dictionary<NetEntity, CameraMarker> Cameras, string ActiveAddress, HashSet<string> AvailableSubnets) _lastState;

    public bool EnableCameraSelection { get; set; }

    public event Action<NetEntity>? CameraSelected;


    public SurveillanceCameraNavMapControl()
    {
        _cameraTexture = _resourceCache.GetTexture("/Textures/Interface/NavMap/beveled_triangle.png");

        TrackedEntitySelectedAction += entity =>
        {
            if (entity.HasValue)
                CameraSelected?.Invoke(entity.Value);
        };
    }

    public void SetActiveCameraAddress(string address)
    {
        if (_activeCameraAddress == address)
            return;

        _activeCameraAddress = address;
        ForceNavMapUpdate();
    }

    public void SetAvailableSubnets(HashSet<string> subnets)
    {
        if (_availableSubnets.SetEquals(subnets))
            return;

        _availableSubnets = subnets;
        ForceNavMapUpdate();
    }

    protected override void UpdateNavMap()
    {
        base.UpdateNavMap();

        if (MapUid is null || !_entityManager.TryGetComponent<SurveillanceCameraMapComponent>(MapUid, out var mapComp))
            return;

        var currentState = (mapComp.Cameras, _activeCameraAddress, _availableSubnets);
        if (_lastState.Equals(currentState))
            return;

        _lastState = currentState;
        UpdateCameraMarkers(mapComp);
    }

    private void UpdateCameraMarkers(SurveillanceCameraMapComponent mapComp)
    {
        TrackedEntities.Clear();

        if (MapUid is null)
            return;

        foreach (var (netEntity, marker) in mapComp.Cameras)
        {
            if (!_availableSubnets.Contains(marker.Subnet))
                continue;

            var coords = new EntityCoordinates(MapUid.Value, marker.Position);

            Color color;
            if (string.IsNullOrEmpty(marker.Address))
                color = CameraInvalidColor;
            else if (marker.Address == _activeCameraAddress)
                color = CameraSelectedColor;
            else
                color = marker.Active ? CameraActiveColor : CameraInactiveColor;

            TrackedEntities[netEntity] = new NavMapBlip(
                coords,
                _cameraTexture,
                color,
                false,
                EnableCameraSelection
            );
        }
    }
}
