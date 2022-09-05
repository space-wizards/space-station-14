using Content.Shared.DeviceNetwork;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Utility;

namespace Content.Client.NetworkConfigurator;

public sealed class NetworkConfiguratorLinkOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public NetworkConfiguratorLinkOverlay()
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        foreach (var tracker in _entityManager.EntityQuery<NetworkConfiguratorActiveLinkOverlayComponent>())
        {
            if (!_entityManager.TryGetComponent(tracker.Owner, out DeviceListComponent? deviceList))
            {
                _entityManager.RemoveComponentDeferred<NetworkConfiguratorActiveLinkOverlayComponent>(tracker.Owner);
                continue;
            }

            var sourceTransform = _entityManager.GetComponent<TransformComponent>(tracker.Owner);

            foreach (var device in deviceList.Devices)
            {
                var linkTransform = _entityManager.GetComponent<TransformComponent>(device);

                args.WorldHandle.DrawLine(sourceTransform.WorldPosition, linkTransform.WorldPosition, Color.Blue);
            }
        }
    }
}
