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

    private readonly Texture _activeTexture;
    private readonly Texture _inactiveTexture;
    private readonly Texture _selectedTexture;
    private readonly Texture _invalidTexture;

    private string _activeCameraAddress = string.Empty;
    private HashSet<string> _availableSubnets = new();
    private (Dictionary<NetEntity, CameraMarker> Cameras, string ActiveAddress, HashSet<string> AvailableSubnets) _lastState;

    public bool EnableCameraSelection { get; set; }

    public event Action<NetEntity>? CameraSelected;


    public SurveillanceCameraNavMapControl()
    {
        IoCManager.InjectDependencies(this);

        _activeTexture = _resourceCache.GetTexture("/Textures/Interface/NavMap/beveled_triangle.png");
        _selectedTexture = _activeTexture;
        _inactiveTexture = _resourceCache.GetTexture("/Textures/Interface/NavMap/beveled_circle.png");
        _invalidTexture = _resourceCache.GetTexture("/Textures/Interface/NavMap/beveled_square.png");

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
            if (!marker.Visible || !_availableSubnets.Contains(marker.Subnet))
                continue;

            var coords = new EntityCoordinates(MapUid.Value, marker.Position);

            Texture texture;
            Color color;

            if (string.IsNullOrEmpty(marker.Address))
            {
                color = CameraInvalidColor;
                texture = _invalidTexture;
            }
            else if (marker.Address == _activeCameraAddress)
            {
                color = CameraSelectedColor;
                texture = _selectedTexture;
            }
            else if (marker.Active)
            {
                color = CameraActiveColor;
                texture = _activeTexture;
            }
            else
            {
                color = CameraInactiveColor;
                texture = _inactiveTexture;
            }

            TrackedEntities[netEntity] = new NavMapBlip(
                coords,
                texture,
                color,
                false,
                EnableCameraSelection
            );
        }
    }
}
