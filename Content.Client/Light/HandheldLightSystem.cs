using Content.Client.Items;
using Content.Client.Light.Components;
using Content.Shared.Light;
using Content.Shared.Light.Component;

namespace Content.Client.Light;

public sealed class HandheldLightSystem : SharedHandheldLightSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HandheldLightComponent, ItemStatusCollectMessage>(OnGetStatusControl);
    }
    
    private static void OnGetStatusControl(EntityUid uid, HandheldLightComponent component, ItemStatusCollectMessage args)
    {
        args.Controls.Add(new HandheldLightStatus(component));
    }
}
