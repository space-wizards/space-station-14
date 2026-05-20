using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.Mind;
using Content.Shared.ParadoxClone;
using Content.Shared.Popups;
using Content.Shared.Radio.Components;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared.ParadoxClone;

/// <summary>
///  Handles everything related to paradox clones that isn't rule-related
/// </summary>
public abstract class SharedParadoxCloneSystem : EntitySystem
{
    [Dependency] protected SharedMindSystem _mindSystem = default!;
    [Dependency] protected SharedTransformSystem _transformSystem = default!;
    [Dependency] protected IEntityManager _entMan = default!;
    [Dependency] protected SharedActionsSystem _actions = default!;
    [Dependency] protected SharedContainerSystem _containers = default!;
    [Dependency] protected readonly SharedPopupSystem _popup = default!;

    [Dependency] protected AlertsSystem _alerts = default!;

    private static readonly EntProtoId ActionSpawn = "ActionParadoxCloneMaterialize";
    private static readonly ProtoId<AlertPrototype> Alert = "ParadoxHourglass";

    private static readonly int AlertSeverityCount = 4;

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
            // Display the remaining time alert. We use ceil so that it only reaches the 0 remaining time alert when the time is effectively 0
            var severity = (short)Math.Ceiling(((ent.Comp.MaxWanderTime - ent.Comp.WanderTime) / ent.Comp.MaxWanderTime) * AlertSeverityCount);
            _alerts.ShowAlert(ent.Owner, Alert, severity);

            ent.Comp.WanderTime -= frameTime;
            if (ent.Comp.WanderTime < 0f)
            {
                // force entity to spawn
                Materialize(ent);
                _popup.PopupEntity(Loc.GetString("paradox-clone-force-spawn"), ent.Owner, ent.Owner, PopupType.MediumCaution);
            }
        }
        else
        {
            // Display the remaining time alert. We use ceil so that it only reaches the 0 remaining time alert when the time is effectively 0
            var severity = (short)Math.Ceiling(((ent.Comp.MaxListenTime - ent.Comp.ListenTime) / ent.Comp.MaxListenTime) * AlertSeverityCount);
            _alerts.ShowAlert(ent.Owner, Alert, severity);

            ent.Comp.ListenTime -= frameTime;
            if (ent.Comp.ListenTime < 0f)
            {
                // force entity to wander
                Wander(ent);
                _popup.PopupEntity(Loc.GetString("paradox-clone-force-wander"), ent.Owner, ent.Owner, PopupType.MediumCaution);
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
    protected virtual void Materialize(Entity<ParadoxCloneComponent> ent)
    {
        // clear ALL actions (one of these should be the spawn action)
        var actions = _actions.GetActions(ent.Owner);
        foreach (var action in actions)
        {
            _actions.RemoveAction((action.Owner, action.Comp));
        }
        // we dont want it to remain forever
        _alerts.ClearAlert(ent.Owner, Alert);
    }
}
