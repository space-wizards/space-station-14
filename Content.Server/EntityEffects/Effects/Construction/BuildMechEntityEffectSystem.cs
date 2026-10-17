using Content.Server.Construction;
using Content.Server.Mech.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.Mech.Components;
using Content.Shared.Power.Components;
using Robust.Server.Containers;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server.EntityEffects.Effects.Construction;

/// <summary>
/// Builds a mech
/// </summary>
public sealed partial class BuildMechEntityEffectSystem : EntityEffectSystem<TransformComponent, BuildMech>
{
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private MechSystem _mech = default!;
    [Dependency] private IEntityManager _entMan = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<BuildMech> args)
    {
        if (!TryComp<ContainerManagerComponent>(entity.Owner, out var containerManager))
        {
            Log.Warning($"Mech construct entity {ToPrettyString(entity.Owner)} did not have a container manager! Aborting build mech action.");
            return;
        }

        if (!_container.TryGetContainer(entity.Owner, args.Effect.Container, out var container, containerManager))
        {
            Log.Warning($"Mech construct entity {ToPrettyString(entity.Owner)} did not have the specified '{args.Effect.Container}' container! Aborting build mech action.");
            return;
        }

        if (container.ContainedEntities.Count != 1)
        {
            Log.Warning($"Mech construct entity {entity.Owner} did not have exactly one item in the specified '{args.Effect.Container}' container! Aborting build mech action.");
        }

        var cell = container.ContainedEntities[0];

        if (!TryComp<BatteryComponent>(cell, out var batteryComponent))
        {
            Log.Warning($"Mech construct entity {entity.Owner} had an invalid entity in container \"{args.Effect.Container}\"! Aborting build mech action.");
            return;
        }

        _container.Remove(cell, container);

        var mech = Spawn(args.Effect.MechPrototype, entity.Comp.Coordinates);

        if (TryComp<MechComponent>(mech, out var mechComp) && mechComp.BatterySlot.ContainedEntity == null)
        {
            _mech.InsertBattery(mech, cell, mechComp, batteryComponent);
            _container.Insert(cell, mechComp.BatterySlot);
        }

        var entChangeEv = new ConstructionChangeEntityEvent(mech, entity.Owner);
        _entMan.EventBus.RaiseLocalEvent(entity.Owner, entChangeEv);
        _entMan.EventBus.RaiseLocalEvent(mech, entChangeEv, broadcast: true);
        _entMan.QueueDeleteEntity(entity.Owner);
    }
}

public sealed partial class BuildMech : EntityEffectBase<BuildMech>
{
    [DataField(required: true)]
    public EntProtoId MechPrototype;

    [DataField]
    public string Container = "battery-container";
}
