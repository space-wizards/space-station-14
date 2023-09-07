using Content.Shared.Light;
using Content.Shared.Light.Components;
using Robust.Shared.GameObjects;

namespace Content.Server.Light.EntitySystems;

public sealed class RotatingLightSystem : SharedRotatingLightSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RotatingLightComponent, PointLightToggleEvent>(OnLightToggle);
    }

    private void OnLightToggle(EntityUid uid, RotatingLightComponent comp, PointLightToggleEvent args)
    {
        if (comp.Enabled == args.Enabled)
            return;

        comp.Enabled = args.Enabled;
        Dirty(comp);
    }
}
