using Content.Server.GameTicking.Rules;
using Content.Shared.Actions;
using Content.Shared.Mind;
using Content.Shared.ParadoxClone;
using Content.Shared.Radio.Components;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Served.ParadoxClone;

public sealed partial class ParadoxCloneSystem : EntitySystem
{
    [Dependency]
    private SharedMindSystem _mindSystem = default!;
    [Dependency]
    private SharedTransformSystem _transformSystem = default!;
    [Dependency]
    private IEntityManager _entMan = default!;
    [Dependency]
    private SharedActionsSystem _actions = default!;
    [Dependency]
    private SharedContainerSystem _containers = default!;

    private static readonly EntProtoId ActionSpawn = "ActionParadoxCloneMaterialize";
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ParadoxCloneComponent, ActionParadoxCloneMaterializeEvent>(OnMaterialize);
        SubscribeLocalEvent<ParadoxCloneComponent, ActionParadoxCloneWanderEvent>(OnWander);
    }

    private void OnWander(Entity<ParadoxCloneComponent> ent, ref ActionParadoxCloneWanderEvent args)
    {
        // Replace the wander action by the spawn action
        _actions.RemoveAction(args.Action.Owner);
        _actions.AddAction(ent.Owner, ActionSpawn);

        // Remove the entity from its container
        if (_containers.TryGetContainingContainer(ent.Owner, out var container))
        {
            var owner = container.Owner;
            _containers.Remove(ent.Owner, container);
            _entMan.RemoveComponent<ParadoxClonedEntityComponent>(owner);
        }

        // Remove the paradox clone radio
        _entMan.RemoveComponent<ActiveRadioComponent>(ent.Owner);

        // Makes the entity visible

    }

    /// <summary>
    /// Materializes the paradox clone, removing its ghost entity and spawning its real body.
    /// </summary>
    private void OnMaterialize(Entity<ParadoxCloneComponent> ent, ref ActionParadoxCloneMaterializeEvent args)
    {
        // get the mind to transfer
        if (!_mindSystem.TryGetMind(args.Performer, out var mind, out var mindComp))
            return;

        // unpause the clone, who was paused so that it doesnt die of spacing
        SetPaused((EntityUid)ent.Comp.ClonedBody, false);

        // transfer the mind and retrieve the body from nullspace
        _mindSystem.TransferTo(mind, ent.Comp.ClonedBody);
        _transformSystem.SetMapCoordinates(ent.Comp.ClonedBody, _transformSystem.GetMapCoordinates(ent.Owner));

        // Finally, delete the ghost entity
        _entMan.DeleteEntity(ent.Owner);
    }
}
