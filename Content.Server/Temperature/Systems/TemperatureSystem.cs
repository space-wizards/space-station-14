using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Temperature.Components;
using Content.Shared.Atmos;
using Content.Shared.Inventory;
using Content.Shared.Rejuvenate;
using Content.Shared.Temperature;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Temperature.Systems;

/// <summary>
/// The system responsible for handling the temperature of entities.
/// </summary>
public sealed class TemperatureSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<TemperatureComponent, AtmosExposedUpdateEvent>(OnAtmosExposedUpdate);
        SubscribeLocalEvent<TemperatureComponent, RejuvenateEvent>(OnRejuvenate);
        SubscribeLocalEvent<TemperatureProtectionComponent, InventoryRelayedEvent<ModifyChangedTemperatureEvent>>(OnTemperatureChangeAttempt);
        SubscribeLocalEvent<InternalTemperatureComponent, MapInitEvent>(OnInit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // conduct heat from the surface to the inside of entities with internal temperatures
        var query = EntityQueryEnumerator<InternalTemperatureComponent, TemperatureComponent>();
        while (query.MoveNext(out var uid, out var comp, out var temp))
        {
            // don't do anything if they equalised
            var diff = Math.Abs(temp.CurrentTemperature - comp.Temperature);
            if (diff < 0.1f)
                continue;

            // heat flow in W/m^2 as per fourier's law in 1D.
            var q = comp.Conductivity * diff / comp.Thickness;

            // convert to J then K
            var joules = q * comp.Area * frameTime;
            var degrees = joules / GetHeatCapacity(uid, temp);
            if (temp.CurrentTemperature < comp.Temperature)
                degrees *= -1;

            // exchange heat between inside and surface
            comp.Temperature += degrees;
            ForceChangeTemperature(uid, temp.CurrentTemperature - degrees, temp);
        }
    }

    public void ForceChangeTemperature(EntityUid uid, float temp, TemperatureComponent? temperature = null)
    {
        if (!Resolve(uid, ref temperature))
            return;

        float lastTemp = temperature.CurrentTemperature;
        float delta = temperature.CurrentTemperature - temp;
        temperature.CurrentTemperature = temp;
        RaiseLocalEvent(uid, new OnTemperatureChangeEvent((uid, temperature), temperature.CurrentTemperature, lastTemp, delta), broadcast: true);
    }

    public void ChangeHeat(EntityUid uid, float heatAmount, bool ignoreHeatResistance = false,
        TemperatureComponent? temperature = null)
    {
        if (!Resolve(uid, ref temperature))
            return;

        if (!ignoreHeatResistance)
        {
            var ev = new ModifyChangedTemperatureEvent(heatAmount);
            RaiseLocalEvent(uid, ev);
            heatAmount = ev.TemperatureDelta;
        }

        float lastTemp = temperature.CurrentTemperature;
        temperature.CurrentTemperature += heatAmount / GetHeatCapacity(uid, temperature);
        float delta = temperature.CurrentTemperature - lastTemp;

        RaiseLocalEvent(uid, new OnTemperatureChangeEvent((uid, temperature), temperature.CurrentTemperature, lastTemp, delta), broadcast: true);
    }

    private void OnAtmosExposedUpdate(EntityUid uid, TemperatureComponent temperature,
        ref AtmosExposedUpdateEvent args)
    {
        var transform = args.Transform;

        if (transform.MapUid == null)
            return;

        var temperatureDelta = args.GasMixture.Temperature - temperature.CurrentTemperature;
        var airHeatCapacity = _atmosphere.GetHeatCapacity(args.GasMixture, false);
        var heatCapacity = GetHeatCapacity(uid, temperature);
        var heat = temperatureDelta * (airHeatCapacity * heatCapacity /
                                       (airHeatCapacity + heatCapacity));
        ChangeHeat(uid, heat * temperature.AtmosTemperatureTransferEfficiency, temperature: temperature);
    }

    public float GetHeatCapacity(EntityUid uid, TemperatureComponent? comp = null, PhysicsComponent? physics = null)
    {
        if (!Resolve(uid, ref comp) || !Resolve(uid, ref physics, false) || physics.FixturesMass <= 0)
        {
            return Atmospherics.MinimumHeatCapacity;
        }

        return comp.SpecificHeat * physics.FixturesMass;
    }

    private void OnInit(EntityUid uid, InternalTemperatureComponent comp, MapInitEvent args)
    {
        if (!TryComp<TemperatureComponent>(uid, out var temp))
            return;

        comp.Temperature = temp.CurrentTemperature;
    }

    private void OnRejuvenate(EntityUid uid, TemperatureComponent comp, RejuvenateEvent args)
    {
        ForceChangeTemperature(uid, Atmospherics.T20C, comp);
    }

    private void OnTemperatureChangeAttempt(EntityUid uid, TemperatureProtectionComponent component,
        InventoryRelayedEvent<ModifyChangedTemperatureEvent> args)
    {
        var ev = new GetTemperatureProtectionEvent(component.Coefficient);
        RaiseLocalEvent(uid, ref ev);

        args.Args.TemperatureDelta *= ev.Coefficient;
    }
}

public sealed class OnTemperatureChangeEvent : EntityEventArgs
{
    public readonly Entity<TemperatureComponent> Entity;
    public float CurrentTemperature { get; }
    public float LastTemperature { get; }
    public float TemperatureDelta { get; }

    public OnTemperatureChangeEvent(Entity<TemperatureComponent> entity, float current, float last, float delta)
    {
        Entity = entity;
        CurrentTemperature = current;
        LastTemperature = last;
        TemperatureDelta = delta;
    }
}
