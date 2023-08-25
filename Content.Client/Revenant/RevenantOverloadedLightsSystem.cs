using Content.Shared.Revenant.Components;
using Content.Shared.Revenant.EntitySystems;

namespace Content.Client.Revenant;

public sealed class RevenantOverloadedLightsSystem : SharedRevenantOverloadedLightsSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RevenantOverloadedLightsComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<RevenantOverloadedLightsComponent, ComponentShutdown>(OnShutdown);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var enumerator = EntityQueryEnumerator<RevenantOverloadedLightsComponent, SharedPointLightComponent>();

        while (enumerator.MoveNext(out var comp, out var light))
        {
            //this looks cool :HECK:
            light.Energy = 2f * Math.Abs((float) Math.Sin(0.25 * Math.PI * comp.Accumulator));
        }
    }

    private void OnStartup(EntityUid uid, RevenantOverloadedLightsComponent component, ComponentStartup args)
    {
        var light = EnsureComp<SharedPointLightComponent>(uid);
        component.OriginalEnergy = light.Energy;
        component.OriginalEnabled = light.Enabled;

        light.Enabled = component.OriginalEnabled;
        Dirty(light);
    }

    private void OnShutdown(EntityUid uid, RevenantOverloadedLightsComponent component, ComponentShutdown args)
    {
        if (!TryComp<SharedPointLightComponent>(component.Owner, out var light))
            return;

        if (component.OriginalEnergy == null)
        {
            RemComp<SharedPointLightComponent>(component.Owner);
            return;
        }

        light.Energy = component.OriginalEnergy.Value;
        light.Enabled = component.OriginalEnabled;
        Dirty(light);
    }

    protected override void OnZap(RevenantOverloadedLightsComponent component)
    {

    }
}
