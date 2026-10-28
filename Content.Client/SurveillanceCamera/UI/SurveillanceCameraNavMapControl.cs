using Content.Client.Pinpointer.UI;
using Content.Client.Resources;
using Content.Shared.DeviceNetwork;
using Content.Shared.SurveillanceCamera;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client.SurveillanceCamera.UI;

public sealed partial class SurveillanceCameraNavMapControl : NavMapControl
{
    [Dependency] private IResourceCache _resourceCache = default!;

    private static readonly Color CameraActiveColor = Color.FromHex("#FF00FF");
    private static readonly Color CameraInactiveColor = Color.FromHex("#a09f9fff");
    private static readonly Color CameraSelectedColor = Color.FromHex("#fbff19ff");
    private static readonly Color CameraInvalidColor = Color.FromHex("#fa1f1fff");

    private readonly Texture _activeTexture;
    private readonly Texture _inactiveTexture;
    private readonly Texture _selectedTexture;
    private readonly Texture _invalidTexture;

    private DeviceAddress _activeCameraAddress = DeviceAddress.Invalid;
    private HashSet<ProtoId<DeviceFrequencyPrototype>> _availableSubnets = [];
    private Dictionary<NetEntity, CameraMarker> _markers = [];
    private bool _markersDirty = true;

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

    public void SetActiveCameraAddress(DeviceAddress address)
    {
        if (_activeCameraAddress == address)
            return;

        _activeCameraAddress = address;
        _markersDirty = true;
        ForceNavMapUpdate();
    }

    public void SetAvailableSubnets(HashSet<ProtoId<DeviceFrequencyPrototype>> subnets)
    {
        if (_availableSubnets.SetEquals(subnets))
            return;

        _availableSubnets = subnets;
        _markersDirty = true;
        ForceNavMapUpdate();
    }

    /// <summary>
    /// Sets the camera markers for the camera map.
    /// </summary>
    public void SetCameraMarkers(Dictionary<NetEntity, CameraMarker> markers)
    {
        _markers = markers;
        _markersDirty = true;
        ForceNavMapUpdate();
    }

    /// <summary>
    /// Attempts to get the marker for the given camera.
    /// </summary>
    public bool TryGetMarker(NetEntity entity, out CameraMarker marker)
    {
        return _markers.TryGetValue(entity, out marker);
    }

    protected override void UpdateNavMap()
    {
        base.UpdateNavMap();

        if (!_markersDirty)
            return;

        UpdateCameraMarkers();
    }

    private void UpdateCameraMarkers()
    {
        TrackedEntities.Clear();

        if (MapUid is null)
            return;

        _markersDirty = false;

        foreach (var (netEntity, marker) in _markers)
        {
            if (!marker.Visible || !_availableSubnets.Contains(marker.Subnet))
                continue;

            var coords = new EntityCoordinates(MapUid.Value, marker.Position);

            Texture texture;
            Color color;

            if (marker.Address == DeviceAddress.Invalid)
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
