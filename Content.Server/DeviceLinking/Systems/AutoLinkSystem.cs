using Content.Server.DeviceLinking.Components;

namespace Content.Server.DeviceLinking.Systems;

/// <summary>
/// This handles automatically linking autolinked entities at round-start.
/// </summary>
public sealed class AutoLinkSystem : EntitySystem
{
    [Dependency] private readonly DeviceLinkSystem _deviceLinkSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<AutoLinkTransmitterComponent, MapInitEvent>(OnAutoLinkMapInit);
    }

    private void OnAutoLinkMapInit(EntityUid uid, AutoLinkTransmitterComponent component, MapInitEvent args)
    {
        var xform = Transform(uid);

        var query = EntityQueryEnumerator<AutoLinkReceiverComponent>();
        while (query.MoveNext(out var receiverUid, out var receiver))
        {
            if (receiver.AutoLinkChannel != component.AutoLinkChannel)
                continue; // Not ours.

            var rxXform = Transform(receiverUid);

            if (rxXform.GridUid != xform.GridUid)
                continue;

            _deviceLinkSystem.LinkDefaults(null, uid, receiverUid);
        }
    }
}

