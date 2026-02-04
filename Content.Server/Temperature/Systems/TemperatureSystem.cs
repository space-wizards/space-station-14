using Content.Server.Atmos.EntitySystems;
using Content.Server.Temperature.Components;
using Content.Shared.Atmos;
using Content.Shared.Inventory;
using Content.Shared.Rejuvenate;
using Content.Shared.Temperature;
using Content.Shared.Projectiles;
using Content.Shared.Temperature.Components;
using Content.Shared.Temperature.HeatContainers;
using Content.Shared.Temperature.Systems;

namespace Content.Server.Temperature.Systems;

public sealed partial class TemperatureSystem : SharedTemperatureSystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TemperatureComponent, AtmosExposedUpdateEvent>(OnAtmosExposedUpdate);
        SubscribeLocalEvent<TemperatureComponent, RejuvenateEvent>(OnRejuvenate);
        Subs.SubscribeWithRelay<TemperatureProtectionComponent, BeforeHeatExchangeEvent>(OnBeforeHeatExchange, held: false);

        SubscribeLocalEvent<InternalTemperatureComponent, MapInitEvent>(OnInit);

        SubscribeLocalEvent<ChangeTemperatureOnCollideComponent, ProjectileHitEvent>(ChangeTemperatureOnCollide);

        InitializeDamage();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // conduct heat from the surface to the inside of entities with internal temperatures
        var query = EntityQueryEnumerator<InternalTemperatureComponent, TemperatureComponent>();
        while (query.MoveNext(out var uid, out var comp, out var temp))
        {
            // don't do anything if they equalized
            var diff = Math.Abs(temp.CurrentTemperature - comp.Temperature);
            if (diff < 0.1f)
                continue;

            // TODO: Heat containers one day. Currently not worth the effort though.
            var dQ = ConductHeat((uid, temp), comp.Temperature, frameTime, comp.Conductivity, true);
            comp.Temperature -= dQ / GetHeatCapacity(uid, temp);
        }

        UpdateDamage();
    }

    public void ForceChangeTemperature(Entity<TemperatureComponent?> entity, float temp)
    {
        if (!TemperatureQuery.Resolve(entity, ref entity.Comp))
            return;

        var lastTemp = entity.Comp.CurrentTemperature;
        entity.Comp.HeatContainer.Temperature = temp;
        var changeEv = new TemperatureChangedEvent(entity.Comp.CurrentTemperature, lastTemp);
        RaiseLocalEvent(entity, ref changeEv, broadcast: true);
    }

    public override float ConductHeat(Entity<TemperatureComponent?> entity, ref HeatContainer heatContainer, float deltaT, float conductivityMod = 1f, bool ignoreHeatResistance = false)
    {
        if (!TemperatureQuery.Resolve(entity, ref entity.Comp, false)
            || MathHelper.CloseTo(entity.Comp.HeatContainer.Temperature, heatContainer.Temperature))
            return 0f;

        var conductivity = entity.Comp.ThermalConductivity;
        if (!ignoreHeatResistance)
        {
            var ev = new BeforeHeatExchangeEvent(conductivity, entity.Comp.HeatContainer.Temperature < heatContainer.Temperature);
            RaiseLocalEvent(entity, ref ev);
            conductivity = ev.Conductivity;
        }

        var lastTemp = entity.Comp.CurrentTemperature;
        var heatEx = entity.Comp.HeatContainer.ConductHeat(ref heatContainer, deltaT, conductivity);

        var changeEv = new TemperatureChangedEvent(entity.Comp.CurrentTemperature, lastTemp);
        RaiseLocalEvent(entity, ref changeEv, broadcast: true);
        return heatEx;
    }

    public float ConductHeat(Entity<TemperatureComponent?> entity, float temperature, float deltaT, float conductivityMod = 1f, bool ignoreHeatResistance = false)
    {
        if (!TemperatureQuery.Resolve(entity, ref entity.Comp, false)
            || MathHelper.CloseTo(entity.Comp.HeatContainer.Temperature, temperature))
            return 0f;

        var conductivity = entity.Comp.ThermalConductivity * conductivityMod;
        if (!ignoreHeatResistance)
        {
            var ev = new BeforeHeatExchangeEvent(conductivity, entity.Comp.HeatContainer.Temperature < temperature);
            RaiseLocalEvent(entity, ref ev);
            conductivity = ev.Conductivity;
        }

        var lastTemp = entity.Comp.CurrentTemperature;
        var heatEx = entity.Comp.HeatContainer.ConductHeat(temperature, deltaT, conductivity);

        var changeEv = new TemperatureChangedEvent(entity.Comp.CurrentTemperature, lastTemp);
        RaiseLocalEvent(entity, ref changeEv, broadcast: true);
        return heatEx;
    }

    public override float ChangeHeat(Entity<TemperatureComponent?> entity, float heatAmount, bool ignoreHeatResistance = false)
    {
        if (!TemperatureQuery.Resolve(entity, ref entity.Comp, false) || heatAmount == 0f)
            return 0f;

        var conductivity = 1f;
        if (!ignoreHeatResistance)
        {
            var ev = new BeforeHeatExchangeEvent(conductivity, heatAmount > 0);
            RaiseLocalEvent(entity, ref ev );
            heatAmount *= ev.Conductivity;
        }

        var lastTemp = entity.Comp.CurrentTemperature;
        entity.Comp.HeatContainer.AddHeat(heatAmount);

        var changeEv = new TemperatureChangedEvent(entity.Comp.CurrentTemperature, lastTemp);
        RaiseLocalEvent(entity, ref changeEv, broadcast: true);

        return heatAmount;
    }

    private void OnAtmosExposedUpdate(Entity<TemperatureComponent> entity, ref AtmosExposedUpdateEvent args)
    {
        var transform = args.Transform;

        if (transform.MapUid == null)
            return;

        // TODO ATMOS: Atmos heat containers!!!
        // We purposefully do not change the gas mixture's heat because it will cause vacuums to heat up to be 20x hotter than the core of the sun.
        var atmosContainer = new HeatContainer(_atmosphere.GetHeatCapacity(args.GasMixture, false), args.GasMixture.Temperature);
        ConductHeat(entity.AsNullable(), ref atmosContainer, args.DeltaTime, 1f);
    }

    private void OnInit(Entity<InternalTemperatureComponent> entity, ref MapInitEvent args)
    {
        if (!TemperatureQuery.TryComp(entity, out var temp))
            return;

        // TODO: Make this use heat containers as well. Rn I'm lazy and it's only used for the chef who I hate!!!
        entity.Comp.Temperature = temp.HeatContainer.Temperature;
    }

    private void OnRejuvenate(Entity<TemperatureComponent> entity, ref RejuvenateEvent args)
    {
        ForceChangeTemperature(entity.AsNullable(), _thermalRegulatorQuery.CompOrNull(entity)?.NormalBodyTemperature ?? Atmospherics.T20C);
    }

    private void OnBeforeHeatExchange(Entity<TemperatureProtectionComponent> entity, ref BeforeHeatExchangeEvent args)
    {
        var coefficient = args.Heating ? entity.Comp.HeatingCoefficient : entity.Comp.CoolingCoefficient;

        args.Conductivity *= coefficient;
    }

    private void ChangeTemperatureOnCollide(Entity<ChangeTemperatureOnCollideComponent> ent, ref ProjectileHitEvent args)
    {
        ChangeHeat(args.Target, ent.Comp.Heat, ent.Comp.IgnoreHeatResistance);// adjust the temperature
    }
}
