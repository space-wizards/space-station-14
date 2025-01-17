using System.Linq;
using Content.Shared.Bed.Cryostorage;
using Content.Shared.Bed.Sleep;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Events;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared.Starlight.Antags.Abductor;

public abstract class SharedAbductorSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;
    
    public override void Initialize()
    {
        SubscribeLocalEvent<AbductorExperimentatorComponent, EntInsertedIntoContainerMessage>(OnInsertedContainer);
        SubscribeLocalEvent<AbductorExperimentatorComponent, EntRemovedFromContainerMessage>(OnRemovedContainer);
        base.Initialize();
    }

    private void OnRemovedContainer(Entity<AbductorExperimentatorComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.ContainerId)
            return;

        if (ent.Comp.Console == null)
        {
            var xform = EnsureComp<TransformComponent>(ent.Owner);
            var console = _entityLookup.GetEntitiesInRange<AbductorConsoleComponent>(xform.Coordinates, 5, LookupFlags.Approximate | LookupFlags.Dynamic)
                .FirstOrDefault().Owner;
            if (console != default)
                ent.Comp.Console = GetNetEntity(console);
        }
        if (ent.Comp.Console != null && GetEntity(ent.Comp.Console.Value) is var consoleid && TryComp<AbductorConsoleComponent>(consoleid, out var consoleComp))
            UpdateGui(consoleComp.Target, (consoleid, consoleComp));

        _appearance.SetData(ent, AbductorExperimentatorVisuals.Full, args.Container.ContainedEntities.Count > 0);
        Dirty(ent);
    }

    private void OnInsertedContainer(Entity<AbductorExperimentatorComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.ContainerId)
            return;
        if (!Timing.IsFirstTimePredicted)
            return;
        if (ent.Comp.Console == null)
        {
            var xform = EnsureComp<TransformComponent>(ent.Owner);
            var console = _entityLookup.GetEntitiesInRange<AbductorConsoleComponent>(xform.Coordinates, 5, LookupFlags.Approximate | LookupFlags.Dynamic)
                .FirstOrDefault().Owner;
            if (console != default)
                ent.Comp.Console = GetNetEntity(console);
        }
        if (ent.Comp.Console != null && GetEntity(ent.Comp.Console.Value) is var consoleid && TryComp<AbductorConsoleComponent>(consoleid, out var consoleComp))
            UpdateGui(consoleComp.Target, (consoleid, consoleComp));

        _appearance.SetData(ent, AbductorExperimentatorVisuals.Full, args.Container.ContainedEntities.Count > 0);
        Dirty(ent);
    }
    
    protected virtual void UpdateGui(NetEntity? target, Entity<AbductorConsoleComponent> computer)
    {

    }
}

