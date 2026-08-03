using System.Collections.Frozen;
using System.Linq;
using Content.Shared.Placeable;

namespace Content.Shared.Temperature.HeatContainer;

/// <summary>
/// This event is raised on an entity to query for its associated heat containers.
/// </summary>
[ByRefEvent]
public record struct QueryForHeatContainerEvent(Component? Sender)
{

    /// <summary>
    /// The component whose logic send this request.
    /// Used by <see cref="Content.Shared.Temperature.Systems.SharedThermodynamicsSystem"/> to properly filter the query.
    /// Aka ensure a heat container only sees the heat containers connected to it.
    /// </summary>
    public readonly Component? Sender=Sender;

    /// <summary>
    /// Entities placed onto an ItemPlacer are considered external.
    /// When an item placer gets queried we do not want placed entities to further query item place causing potential loops.
    /// Basically all queries raising sub queries should not include externals and any handler that deals with externals, should not respond if externals are excluded
    /// for an example <see cref="ItemPlacerSystem.QueryForHeatContainer"/>
    /// </summary>
    public readonly bool IncludeExternal=(Sender!=null);

    /// <summary>
    /// If set true, do not add a response to this event.
    /// Set True if the event handler takes over responding for the various components in its own way.
    /// </summary>
    public bool Resolved = false;

    /// <summary>
    /// Any receiver of the event, puts its contact into here.
    /// </summary>
    public readonly List<HeatQueryResult> Responses = [];


}

/// <summary>
/// Like QueryForHeatContainerEvent but only suppose to be handled for the listed components.
/// </summary>
/// <param name="Components"></param>
[ByRefEvent]
public record struct QueryComponentsForContainersEvent(Component[] Components,bool IncludeExternal)
{
    public readonly bool IncludeExternal=IncludeExternal;

    public readonly Component[] Components = Components;

    /// <summary>
    /// Any receiver of the event, puts its contact into here.
    /// </summary>
    public readonly List<HeatQueryResult> Responses = [];
}

/// <summary>
/// Contains all relevant information about a container one trying to interact with it, might want.
/// </summary>
/// <param name="Entity">Entity that reported this contact. Use to send update notification</param>
/// <param name="Container">The actual heat container</param>
/// <param name="Component">The owner of the container. Used to more directly feed notification.</param>
/// <param name="Conductivity">An optional estimated conductivity, assuming full contact between the sender and receiver</param>
public record struct HeatQueryResult(EntityUid Entity,Component Component, IHeatContainer Container, float? Conductivity)
{
    public readonly EntityUid Entity=Entity;

    public readonly Component Component = Component;

    public readonly IHeatContainer Container=Container;

    public readonly float? Conductivity=Conductivity;

}



/// <summary>
/// This event is raised on an entity to notify of a change of its reported heat container.
/// Systems that actively monitor and update heat containers can ignore these.
/// But anything that reported a fake container for lack of a native implementation or is otherwise only event driven in its behavior, should process this event.
/// </summary>
/// <param name="Containers">A lookup for containers by component</param>
[ByRefEvent]
public record struct HeatContainerChangedEvent(FrozenDictionary<Component, IHeatContainer[]> Containers)
{
    public readonly FrozenDictionary<Component,IHeatContainer[]> Containers=Containers;

    public HeatContainerChangedEvent(IEnumerable<HeatQueryResult> contacts):this(contacts.GroupBy(e=>e.Component).ToFrozenDictionary(e=>e.Key,e=>e.Select(f=>f.Container).ToArray()))
    {
    }

}
