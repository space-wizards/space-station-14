using Robust.Client.GameObjects;
using Robust.Shared.Map;

namespace Content.Client.Revenant;

public sealed class RevenantAnimatedSystem : EntitySystem
{
    [Dependency] private readonly SharedPointLightSystem _lights = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RevenantAnimatedComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<RevenantAnimatedComponent, ComponentShutdown>(OnShutdown);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var enumerator = EntityQueryEnumerator<RevenantAnimatedComponent>();

        while (enumerator.MoveNext(out var uid, out var comp))
        {
            if (comp.LightOverlay == null)
                continue;
            comp.Accumulator += frameTime;
            _lights.SetEnergy(comp.LightOverlay.Value.Owner, 2f * Math.Abs((float)Math.Sin(0.25 * Math.PI * comp.Accumulator)), comp.LightOverlay.Value.Comp);
        }
    }

    private void OnStartup(EntityUid uid, RevenantAnimatedComponent comp, ComponentStartup args)
    {
        var lightEnt = Spawn(null, new EntityCoordinates(uid, 0, 0));
        var light = AddComp<PointLightComponent>(lightEnt);

        comp.LightOverlay = (lightEnt, light);

        _lights.SetEnabled(lightEnt, true, light);
        _lights.SetColor(lightEnt, comp.LightColor, light);
        _lights.SetRadius(lightEnt, comp.LightRadius, light);
    }

    private void OnShutdown(EntityUid uid, RevenantAnimatedComponent comp, ComponentShutdown args)
    {
        if (comp.LightOverlay != null)
            Del(comp.LightOverlay);
    }
}