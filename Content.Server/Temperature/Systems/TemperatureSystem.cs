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

    protected override void OnMapInit(Entity<TemperatureComponent> entity, ref MapInitEvent args)
    {
        base.OnMapInit(entity, ref args);

        // Force test fails for species so they don't spawn cold!
        if (_thermalRegulatorQuery.TryComp(entity, out var comp))
            entity.Comp.HeatContainer.Temperature = comp.NormalBodyTemperature;
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
            var dQ = ConductHeat((uid, temp), comp.Temperature, frameTime, comp.Conductance, true);
            comp.Temperature -= dQ / temp.HeatCapacity;
        }

        UpdateDamage();
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
        args.GasMixture.Temperature = atmosContainer.Temperature;
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
        SetTemperature(entity.AsNullable(), _thermalRegulatorQuery.CompOrNull(entity)?.NormalBodyTemperature ?? Atmospherics.T20C);
    }

    private void OnBeforeHeatExchange(Entity<TemperatureProtectionComponent> entity, ref BeforeHeatExchangeEvent args)
    {
        args.Conductance *= entity.Comp.Coefficient;
    }

    private void ChangeTemperatureOnCollide(Entity<ChangeTemperatureOnCollideComponent> ent, ref ProjectileHitEvent args)
    {
        ChangeHeat(args.Target, ent.Comp.Heat, ent.Comp.IgnoreHeatResistance);// adjust the temperature
    }
}
