using System.Linq;
using Content.Shared.Actions;
using Content.Shared.Implants.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs;
using Content.Shared.Tag;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.Network;

namespace Content.Shared.Implants;

public abstract class SharedSubdermalImplantSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    public const string BaseStorageId = "storagebase";

    public override void Initialize()
    {
        SubscribeLocalEvent<SubdermalImplantComponent, EntGotInsertedIntoContainerMessage>(OnInsert);
        SubscribeLocalEvent<SubdermalImplantComponent, ContainerGettingRemovedAttemptEvent>(OnRemoveAttempt);
        SubscribeLocalEvent<SubdermalImplantComponent, EntGotRemovedFromContainerMessage>(OnRemove);

        SubscribeLocalEvent<ImplantedComponent, MobStateChangedEvent>(RelayToImplantEvent);
        SubscribeLocalEvent<ImplantedComponent, AfterInteractUsingEvent>(RelayToImplantEvent);
        SubscribeLocalEvent<ImplantedComponent, SuicideEvent>(RelayToImplantEvent);
    }

    private void OnInsert(EntityUid uid, SubdermalImplantComponent component, EntGotInsertedIntoContainerMessage args)
    {
        if (component.ImplantedEntity == null || _net.IsClient)
            return;

        if (!string.IsNullOrWhiteSpace(component.ImplantAction))
        {
            _actionsSystem.AddAction(component.ImplantedEntity.Value, ref component.Action, component.ImplantAction, uid);
        }

        //replace micro bomb with macro bomb
        if (_container.TryGetContainer(component.ImplantedEntity.Value, ImplanterComponent.ImplantSlotId, out var implantContainer) && _tag.HasTag(uid, "MacroBomb"))
        {
            foreach (var implant in implantContainer.ContainedEntities)
            {
                if (_tag.HasTag(implant, "MicroBomb"))
                {
                    _container.Remove(implant, implantContainer);
                    QueueDel(implant);
                }
            }
        }

        var ev = new ImplantImplantedEvent(uid, component.ImplantedEntity.Value);
        RaiseLocalEvent(uid, ref ev);
    }

    private void OnRemoveAttempt(EntityUid uid, SubdermalImplantComponent component, ContainerGettingRemovedAttemptEvent args)
    {
        if (component.Permanent && component.ImplantedEntity != null)
            args.Cancel();
    }

    private void OnRemove(EntityUid uid, SubdermalImplantComponent component, EntGotRemovedFromContainerMessage args)
    {
        if (component.ImplantedEntity == null || Terminating(component.ImplantedEntity.Value))
            return;

        if (component.ImplantAction != null)
            _actionsSystem.RemoveProvidedActions(component.ImplantedEntity.Value, uid);

        if (!_container.TryGetContainer(uid, BaseStorageId, out var storageImplant))
            return;

        var entCoords = Transform(component.ImplantedEntity.Value).Coordinates;

        var containedEntites = storageImplant.ContainedEntities.ToArray();

        foreach (var entity in containedEntites)
        {
            if (Terminating(entity))
                continue;

            _container.RemoveEntity(storageImplant.Owner, entity, force: true, destination: entCoords);
        }
    }

    /// <summary>
    /// Add a list of implants to a person.
    /// Logs any implant ids that don't have <see cref="SubdermalImplantComponent"/>.
    /// </summary>
    public void AddImplants(EntityUid uid, IEnumerable<String> implants)
    {
        var coords = Transform(uid).Coordinates;
        foreach (var id in implants)
        {
            var ent = Spawn(id, coords);
            if (TryComp<SubdermalImplantComponent>(ent, out var implant))
            {
                ForceImplant(uid, ent, implant);
            }
            else
            {
                Log.Warning($"Found invalid starting implant '{id}' on {uid} {ToPrettyString(uid):implanted}");
                Del(ent);
            }
        }
    }

    /// <summary>
    /// Forces an implant into a person
    /// Good for on spawn related code or admin additions
    /// </summary>
    /// <param name="target">The entity to be implanted</param>
    /// <param name="implant"> The implant</param>
    /// <param name="component">The implant component</param>
    public void ForceImplant(EntityUid target, EntityUid implant, SubdermalImplantComponent component)
    {
        //If the target doesn't have the implanted component, add it.
        var implantedComp = EnsureComp<ImplantedComponent>(target);
        var implantContainer = implantedComp.ImplantContainer;

        component.ImplantedEntity = target;
        _container.Insert(implant, implantContainer);
    }

    /// <summary>
    /// Force remove a singular implant
    /// </summary>
    /// <param name="target">the implanted entity</param>
    /// <param name="implant">the implant</param>
    [PublicAPI]
    public void ForceRemove(EntityUid target, EntityUid implant)
    {
        if (!TryComp<ImplantedComponent>(target, out var implanted))
            return;

        var implantContainer = implanted.ImplantContainer;

        _container.Remove(implant, implantContainer);
        QueueDel(implant);
    }

    /// <summary>
    /// Removes and deletes implants by force
    /// </summary>
    /// <param name="target">The entity to have implants removed</param>
    [PublicAPI]
    public void WipeImplants(EntityUid target)
    {
        if (!TryComp<ImplantedComponent>(target, out var implanted))
            return;

        var implantContainer = implanted.ImplantContainer;

        _container.CleanContainer(implantContainer);
    }

    //Relays from the implanted to the implant
    private void RelayToImplantEvent<T>(EntityUid uid, ImplantedComponent component, T args) where T : notnull
    {
        if (!_container.TryGetContainer(uid, ImplanterComponent.ImplantSlotId, out var implantContainer))
            return;

        var relayEv = new ImplantRelayEvent<T>(args);
        foreach (var implant in implantContainer.ContainedEntities)
        {
            if (args is HandledEntityEventArgs { Handled : true })
                return;

            RaiseLocalEvent(implant, relayEv);
        }
    }
}

public sealed class ImplantRelayEvent<T> where T : notnull
{
    public readonly T Event;

    public ImplantRelayEvent(T ev)
    {
        Event = ev;
    }
}

/// <summary>
/// Event that is raised whenever someone is implanted with any given implant.
/// Raised on the the implant entity.
/// </summary>
/// <remarks>
/// implant implant implant implant
/// </remarks>
[ByRefEvent]
public readonly struct ImplantImplantedEvent
{
    public readonly EntityUid Implant;
    public readonly EntityUid? Implanted;

    public ImplantImplantedEvent(EntityUid implant, EntityUid? implanted)
    {
        Implant = implant;
        Implanted = implanted;
    }
}
