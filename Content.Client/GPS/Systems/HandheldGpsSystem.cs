using Content.Client.GPS.Components;
using Content.Client.GPS.UI;
using Content.Client.Items;

namespace Content.Client.GPS.Systems;

public sealed class HandheldGpsSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HandheldGPSComponent, ItemStatusCollectMessage>(OnItemStatus);
    }

    private void OnItemStatus(EntityUid uid, HandheldGPSComponent component, ItemStatusCollectMessage args)
    {
        args.Controls.Add(new HandheldGpsStatusControl(component));
    }
}
