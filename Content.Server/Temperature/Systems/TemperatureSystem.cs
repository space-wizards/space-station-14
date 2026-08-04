using System.Linq;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Temperature.Components;
using Content.Shared.Atmos;
using Content.Shared.Inventory;
using Content.Shared.Rejuvenate;
using Content.Shared.Temperature;
using Content.Shared.Projectiles;
using Content.Shared.Temperature.Components;
using Content.Shared.Temperature.HeatContainer;
using Content.Shared.Temperature.Systems;

namespace Content.Server.Temperature.Systems;

public sealed partial class TemperatureSystem : SharedTemperatureSystem
{
    [Dependency] private AtmosphereSystem _atmosphere = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TemperatureComponent, AtmosExposedUpdateEvent>(OnAtmosExposedUpdate);
        SubscribeLocalEvent<TemperatureComponent, RejuvenateEvent>(OnRejuvenate);
        Subs.SubscribeWithRelay<TemperatureProtectionComponent, BeforeHeatExchangeEvent>(OnBeforeHeatExchange,
            held: false);

        SubscribeLocalEvent<InternalTemperatureComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<ChangeTemperatureOnCollideComponent, ProjectileHitEvent>(ChangeTemperatureOnCollide);
        SubscribeLocalEvent<InternalTemperatureComponent, QueryForHeatContainerEvent>(
            QueryInternalTempForHeatContainer);
        InitializeDamage();
    }

    private void QueryInternalTempForHeatContainer(EntityUid uid,
        InternalTemperatureComponent component,
        QueryForHeatContainerEvent args)
    {
        if (args.Resolved)
            return;
        //hide if we have a temperature component. -> conduction goes through that one first.
        if (TemperatureQuery.HasComp(uid))
        {
            return;
        }

        args.Responses.Add(new(uid, component, component, null));
    }

    protected override void OnMapInit(Entity<TemperatureComponent> entity, ref MapInitEvent args)
    {
        base.OnMapInit(entity, ref args);

        // Make sure entities don't spawn cold!
        if (_thermalRegulatorQuery.TryComp(entity, out var comp))
            entity.Comp.Temperature = comp.NormalBodyTemperature;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // conduct heat from the surface to the inside of entities with internal temperatures
        var query = EntityQueryEnumerator<InternalTemperatureComponent, TemperatureComponent>();
        while (query.MoveNext(out var uid, out var comp, out var temp))
        {
            // don't do anything if they equalized
            var diff = Math.Abs(temp.Temperature - comp.Temperature);
            if (diff < 0.1f)
                continue;
            //conduct heat between inner temp and outer comp
            ConductHeat((uid, temp), ref comp, frameTime, comp.Conductance, true);
        }

        // now process anything else currently contained/attached to this entity.
        var query2 = EntityQueryEnumerator<TemperatureComponent>();
        while (query2.MoveNext(out var uid, out var comp))
        {
            //TODO instead of doing a query each update, just cache the slots, solutions and components in the temperature component and well use that.
            var containerQuery = new QueryForHeatContainerEvent(comp);
            RaiseLocalEvent(uid, ref containerQuery);
            //this contains by elimination everything but internal temperature with a heat container.
            var minTemp = containerQuery.Responses.Min(e => e.Container.TemperatureC);
            var maxTemp = containerQuery.Responses.Max(e => e.Container.TemperatureC);
            //ignore an almost equalized system.
            if (Math.Abs(maxTemp - minTemp) <
                5) //maybe instead of 5 pick 0.1 * comp.Temp basically scale the tolerance the larger the temp gets.
            {
                continue;
            }

            var noticeEvent = new HeatContainerChangedEvent(containerQuery.Responses);

            var oldTem = comp.Temperature;
            //conduct heat between the temperature container and all touching containers.
            foreach (var response in containerQuery.Responses)
            {
                var responseContainer = response.Container;
                if (Math.Abs((comp as IHeatContainer).TemperatureC - responseContainer.TemperatureC) < 2.5)
                    continue;
                HeatContainerHelpers.ConductHeat(ref comp,
                    ref responseContainer,
                    frameTime,
                    response.Conductivity ?? comp.ThermalConductivity);
            }

            //notify changes of the containers
            foreach (var entityUid in containerQuery.Responses.Select(e => e.Entity).Distinct())
            {
                RaiseLocalEvent(entityUid, ref noticeEvent);
            }

            //update temperature.
            if (Math.Abs(oldTem - (comp.Temperature)) >0)
            {
                var changeEv = new TemperatureChangedEvent(comp.Temperature, oldTem);
                RaiseLocalEvent(uid, ref changeEv, broadcast: true);
            }
        }

        UpdateDamage();
    }

    private void OnAtmosExposedUpdate(Entity<TemperatureComponent> entity, ref AtmosExposedUpdateEvent args)
    {
        var transform = args.Transform;

        if (transform.MapUid == null)
            return;

        // TODO ATMOS: Atmos heat containers!!!
        var atmosContainer = new HeatContainer(_atmosphere.GetHeatCapacity(args.GasMixture, false),
            args.GasMixture.Temperature);
        ConductHeat(entity.AsNullable(), ref atmosContainer, args.DeltaTime, args.ConductivityMod);
        args.GasMixture.Temperature = atmosContainer.Temperature;
    }

    private void OnInit(Entity<InternalTemperatureComponent> entity, ref MapInitEvent args)
    {
        if (!TemperatureQuery.TryComp(entity, out var temp))
            return;

        // TODO: This shouldn't copy temperature component, but this component is so niche it's not worth the effort of fixing.
        entity.Comp.Temperature = temp.Temperature;
        entity.Comp.HeatCapacity = temp.HeatCapacity;
    }

    private void OnRejuvenate(Entity<TemperatureComponent> entity, ref RejuvenateEvent args)
    {
        SetTemperature(entity.AsNullable(),
            _thermalRegulatorQuery.CompOrNull(entity)?.NormalBodyTemperature ?? Atmospherics.T20C);
    }

    private void OnBeforeHeatExchange(Entity<TemperatureProtectionComponent> entity, ref BeforeHeatExchangeEvent args)
    {
        // TODO: Proper coverage modifiers!!! This should be its own system which relays to inventory and then based on coverage spits out a modifier!
        args.HeatTransferModifier *= entity.Comp.Coefficient;
    }

    private void ChangeTemperatureOnCollide(Entity<ChangeTemperatureOnCollideComponent> ent,
        ref ProjectileHitEvent args)
    {
        ChangeHeat(args.Target, ent.Comp.Heat, ent.Comp.IgnoreHeatResistance); // adjust the temperature
    }
}
