using Content.Server.GameTicking.Rules;
using Content.Shared.Actions;
using Content.Shared.Mind;
using Content.Shared.ParadoxClone;
using Content.Shared.Popups;
using Content.Shared.Radio.Components;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server.ParadoxClone;

/// <summary>
///  Handles everything related to paradox clones that isn't rule-related
/// </summary>
public sealed partial class ParadoxCloneSystem : EntitySystem
{
    [Dependency] private SharedMindSystem _mindSystem = default!;
    [Dependency] private SharedTransformSystem _transformSystem = default!;
    [Dependency] private IEntityManager _entMan = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private static readonly EntProtoId ActionSpawn = "ActionParadoxCloneMaterialize";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ParadoxCloneComponent, ActionParadoxCloneMaterializeEvent>(OnMaterialize);
        SubscribeLocalEvent<ParadoxCloneComponent, ActionParadoxCloneWanderEvent>(OnWander);
    }

    // Handles forcing entities to spawn & wander
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = _entMan.EntityQueryEnumerator<ParadoxCloneComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            TimePass((uid, comp), frameTime);
        }
    }

    private void TimePass(Entity<ParadoxCloneComponent> ent, float frameTime)
    {
        if (ent.Comp.IsWandering)
        {
            ent.Comp.WanderTime -= frameTime;
            if (ent.Comp.WanderTime < 0f)
            {
                // force entity to spawn
                Materialize(ent);
                _popup.PopupEntity("paradox-clone-force-spawn", ent.Owner, ent.Owner, PopupType.MediumCaution);
            }
        }
        else
        {
            ent.Comp.ListenTime -= frameTime;
            if (ent.Comp.ListenTime < 0f)
            {
                // force entity to wander
                Wander(ent);
                _popup.PopupEntity("paradox-clone-force-wander", ent.Owner, ent.Owner, PopupType.MediumCaution);
            }
        }
    }

    private void OnWander(Entity<ParadoxCloneComponent> ent, ref ActionParadoxCloneWanderEvent args)
    {
        Wander(ent);
    }

    /// <summary>
    /// Makes a paradox clone entity wander
    /// </summary>
    private void Wander(Entity<ParadoxCloneComponent> ent)
    {
        // clear ALL actions (one of these should be the wander action)
        var actions = _actions.GetActions(ent.Owner);
        foreach (var action in actions)
        {
            _actions.RemoveAction((action.Owner, action.Comp));
        }

         // Remove the entity from its container
        if (_containers.TryGetContainingContainer(ent.Owner, out var container))
        {
            var owner = container.Owner;
            _containers.Remove(ent.Owner, container);
            _entMan.RemoveComponent<ParadoxClonedEntityComponent>(owner);
        }

        // Remove the paradox clone radio
        _entMan.RemoveComponent<ActiveRadioComponent>(ent.Owner);
        ent.Comp.IsWandering = true;

        // Give it the spawn action
        _actions.AddAction(ent.Owner, ActionSpawn);
    }

    private void OnMaterialize(Entity<ParadoxCloneComponent> ent, ref ActionParadoxCloneMaterializeEvent args)
    {
        Materialize(ent);
    }

    /// <summary>
    /// Materializes the paradox clone, removing its ghost entity and spawning its real body.
    /// </summary>
    private void Materialize(Entity<ParadoxCloneComponent> ent)
    {
         // clear ALL actions (one of these should be the spawn action)
        var actions = _actions.GetActions(ent.Owner);
        foreach (var action in actions)
        {
            _actions.RemoveAction((action.Owner, action.Comp));
        }

        // get the mind to transfer
        if (!_mindSystem.TryGetMind(ent.Owner, out var mind, out var mindComp))
            return;

        // unpause the clone, who was paused so that it doesnt die of spacing
        SetPaused((EntityUid)ent.Comp.ClonedBody, false);

        // transfer the mind and retrieve the body from nullspace
        _mindSystem.TransferTo(mind, ent.Comp.ClonedBody);
        _transformSystem.SetMapCoordinates(ent.Comp.ClonedBody, _transformSystem.GetMapCoordinates(ent.Owner));

        // Finally, delete the ghost entity (we dont need to set IsWandering off because that component will be deleted alongside its entity)
        _entMan.DeleteEntity(ent.Owner);
    }
}
