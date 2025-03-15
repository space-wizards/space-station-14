using Content.Shared.Revenant.Components;
using Content.Shared.Revenant.Systems;
using Robust.Client.GameObjects;

namespace Content.Client.Revenant.Systems;

public sealed class RevenantOverloadedLightsSystem : SharedRevenantOverloadedLightsSystem
{
    [Dependency] private readonly SharedPointLightSystem _lights = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RevenantOverloadedLightsComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<RevenantOverloadedLightsComponent, ComponentShutdown>(OnShutdown);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var enumerator = EntityQueryEnumerator<RevenantOverloadedLightsComponent, PointLightComponent>();

        while (enumerator.MoveNext(out var uid, out var comp, out var light))
        {
            //this looks cool :HECK:
            _lights.SetEnergy(uid, 2f * Math.Abs((float) Math.Sin(0.25 * Math.PI * comp.NextZapTime.TotalSeconds)), light);
        }
    }

    private void OnStartup(Entity<RevenantOverloadedLightsComponent> ent, ref ComponentStartup args)
    {
        var light = _lights.EnsureLight(ent);
        ent.Comp.OriginalEnergy = light.Energy;
        ent.Comp.OriginalEnabled = light.Enabled;

        _lights.SetEnabled(ent, ent.Comp.OriginalEnabled, light);
        Dirty(ent, light);
    }

    private void OnShutdown(Entity<RevenantOverloadedLightsComponent> ent, ref ComponentShutdown args)
    {
        if (!_lights.TryGetLight(ent, out var light))
            return;

        if (ent.Comp.OriginalEnergy is not { } originalEnergy)
        {
            RemComp(ent, light);
            return;
        }

        _lights.SetEnergy(ent, originalEnergy, light);
        _lights.SetEnabled(ent, ent.Comp.OriginalEnabled, light);
        Dirty(ent, light);
    }

    protected override void OnZap(Entity<RevenantOverloadedLightsComponent> component)
    {
    }
}
