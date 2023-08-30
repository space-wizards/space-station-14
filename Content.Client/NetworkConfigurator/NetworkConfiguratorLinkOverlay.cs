using Content.Client.NetworkConfigurator.Systems;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Components;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Random;
using Robust.Shared.Map;

namespace Content.Client.NetworkConfigurator;

public sealed class NetworkConfiguratorLinkOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    private readonly DeviceListSystem _deviceListSystem;

    private Dictionary<EntityUid, Color> _colors = new();

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public NetworkConfiguratorLinkOverlay()
    {
        IoCManager.InjectDependencies(this);

        _deviceListSystem = _entityManager.System<DeviceListSystem>();
    }

    public void ClearEntity(EntityUid uid)
    {
        _colors.Remove(uid);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        foreach (var tracker in _entityManager.EntityQuery<NetworkConfiguratorActiveLinkOverlayComponent>())
        {
            if (_entityManager.Deleted(tracker.Owner) || !_entityManager.TryGetComponent(tracker.Owner, out DeviceListComponent? deviceList))
            {
                _entityManager.RemoveComponentDeferred<NetworkConfiguratorActiveLinkOverlayComponent>(tracker.Owner);
                continue;
            }

            if (!_colors.TryGetValue(tracker.Owner, out var color))
            {
                color = new Color(
                    _random.Next(0, 255),
                    _random.Next(0, 255),
                    _random.Next(0, 255));
                _colors.Add(tracker.Owner, color);
            }

            var sourceTransform = _entityManager.GetComponent<TransformComponent>(tracker.Owner);
            if (sourceTransform.MapID == MapId.Nullspace)
            {
                // Can happen if the item is outside the client's view. In that case,
                // we don't have a sensible transform to draw, so we need to skip it.
                continue;
            }

            foreach (var device in _deviceListSystem.GetAllDevices(tracker.Owner, deviceList))
            {
                if (_entityManager.Deleted(device))
                {
                    continue;
                }

                var linkTransform = _entityManager.GetComponent<TransformComponent>(device);
                if (linkTransform.MapID == MapId.Nullspace)
                {
                    continue;
                }

                args.WorldHandle.DrawLine(sourceTransform.WorldPosition, linkTransform.WorldPosition, _colors[tracker.Owner]);
            }
        }
    }
}
