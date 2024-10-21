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
        Dirty(uid, comp);
    }

    public void SetEnabled(EntityUid uid, RotatingLightComponent comp, bool enabled)
    {
        comp.Enabled = enabled;
        Dirty(uid, comp);
    }
}
