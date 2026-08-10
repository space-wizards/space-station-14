using System.Linq;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Temperature.Components;
using Content.Shared.Atmos;
using Content.Shared.Temperature.HeatContainer;
using Content.Shared.Temperature.Systems;

namespace Content.Server.Temperature.Systems;

public sealed partial class GeneralThermodynamicsSystem : EntitySystem
{
    [Dependency] private HeatContainerQuerySystem _querySystem = default!;
    [Dependency] private AtmosphereSystem _atmosphere = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GeneralThermodynamicsComponent, AtmosExposedUpdateEvent>(AtmosExposedUpdated);

        base.Initialize();
    }

    private void AtmosExposedUpdated(EntityUid uid,
        GeneralThermodynamicsComponent component,
        AtmosExposedUpdateEvent args)
    {
        var airHeat = new HeatContainer(_atmosphere.GetHeatCapacity(args.GasMixture, false),
            args.GasMixture.Temperature);
        foreach (var exposure in component.Exposures)
        {
            foreach (var container in _querySystem.FindContainer(exposure.AddressA, uid, true))
            {
                var data = container;
                if (HeatContainerHelpers.ConductHeat(ref airHeat,
                        ref data,
                        args.DeltaTime,
                        exposure.Conductivity ?? args.ConductivityMod) != 0)
                {
                    _querySystem.ApplyHeatContainer(data);
                }
            }
        }

        args.GasMixture.Temperature = airHeat.Temperature;
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<GeneralThermodynamicsComponent>();
        while (query.MoveNext(out var entity, out var comp))
        {
            //a cache, which might be partially pushed into the component (at least the static containers)
            Dictionary<HeatContainerQuerySystem.HeatContainerAddress, List<IHeatContainer>> heatContainers = [];

            HashSet<IHeatContainer> changed = [];
            //exchange heat across connections.
            foreach (var connection in comp.Connections)
            {
                if (!heatContainers.TryGetValue(connection.AddressA, out var containerA))
                {
                    containerA = _querySystem.FindContainer(connection.AddressA, entity, true).ToList();
                    heatContainers.Add(connection.AddressA, containerA);
                }

                if (!heatContainers.TryGetValue(connection.AddressB, out var containerB))
                {
                    containerB = _querySystem.FindContainer(connection.AddressB, entity, true).ToList();
                    heatContainers.Add(connection.AddressB, containerB);
                }

                foreach (var a in containerA)
                {
                    foreach (var b in containerB)
                    {
                        if (a == b)
                            continue;
                        var aA = a;
                        var bB = b;
                        if (0 != HeatContainerHelpers.ConductHeat(ref aA, ref bB, frameTime, connection.Conductivity))
                        {
                            changed.Add(a);
                            changed.Add(bB);
                        }
                    }
                }
            }

            //exchange heat in a slot.
            foreach (var pool in comp.SelfMix)
            {
                if (!heatContainers.TryGetValue(pool.AddressA, out var containerA))
                {
                    containerA = _querySystem.FindContainer(pool.AddressA, entity, true).ToList();
                    heatContainers.Add(pool.AddressA, containerA);
                }

                for (var i = 0; i < containerA.Count - 1; i++)
                {
                    for (var j = i + 1; j < containerA.Count; j++)
                    {
                        var a = containerA[i];
                        var b = containerA[j];
                        if (a == b)
                            continue;
                        if (0 != HeatContainerHelpers.ConductHeat(ref a, ref b, frameTime, pool.Conductivity!.Value))
                        {
                            changed.Add(a);
                            changed.Add(b);
                        }
                    }
                }
            }

            //send updates to all changed.
            _querySystem.ApplyBoxedContainers(changed);
        }
    }
}
