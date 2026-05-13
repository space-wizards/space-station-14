using Content.Shared.Implants;
using Content.Shared.Inventory;
using Content.Shared.Mindshield.Components;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;

namespace Content.Shared.Mindshield;

public abstract class SharedMindShieldSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        // Mind shield events
        // Status
        SubscribeLocalEvent<MindShieldComponent, ImplantRelayEvent<QueryMindShieldStatusEvent>>((_, ref k) => k.Args.IsMindshielded = true);
        SubscribeLocalEvent<MindShieldComponent, InventoryRelayedEvent<QueryMindShieldStatusEvent>>((_, ref k) => k.Args.IsMindshielded = true);
        SubscribeLocalEvent<MindShieldComponent, QueryMindShieldStatusEvent>((_, ref k) => k.IsMindshielded = true);
        // Visuals
        SubscribeLocalEvent<MindShieldComponent, ImplantRelayEvent<QueryMindShieldVisualsEvent>>((a, ref k) => OnQueryMindShieldVisuals(a, ref k.Args));
        SubscribeLocalEvent<MindShieldComponent, InventoryRelayedEvent<QueryMindShieldVisualsEvent>>((a, ref k) => OnQueryMindShieldVisuals(a, ref k.Args));
        SubscribeLocalEvent<MindShieldComponent, QueryMindShieldVisualsEvent>(OnQueryMindShieldVisuals);

        // TODO
    }

    private void OnQueryMindShieldVisuals(Entity<MindShieldComponent> a, ref QueryMindShieldVisualsEvent k)
    {
        k.IsVisible = true;
        // Apply the visuals. We check the priority so that things like fake mindshields always get overwritten by real mindshields
        if (a.Comp.VisualPriority > k.Priority)
        {
            k.Priority = a.Comp.VisualPriority;
            k.MindShieldStatusIcon = a.Comp.MindShieldStatusIcon;
        }
    }

    /// <summary>
    /// Used to know if an entity is mindshielded - this means that the entity has protection from mind control (such as from revolutionary flashes).
    /// </summary>
    /// <param name="entity">The entity to check the mindshield status of.</param>
    /// <returns>True if a mindshield-granting device is found & is active, false otherwise.</returns>
    /// <remarks>You should never look for a mindshield component and instead use this function.</remarks>
    public bool IsMindshielded(EntityUid entity)
    {
        var ev = new QueryMindShieldStatusEvent();
        RaiseLocalEvent(entity, ref ev);
        return ev.IsMindshielded;
    }

}

/// <summary>
/// Raised in order to query wether an entity is mindshielded
/// </summary>
[ByRefEvent]
public sealed class QueryMindShieldStatusEvent : EntityEventArgs, IInventoryRelayEvent
{
    public SlotFlags TargetSlots => SlotFlags.All;
    /// <summary>
    /// Wether the entity is mindshielded.
    /// </summary>
    public bool IsMindshielded = false;
}
/// <summary>
/// Raised in order to query wether an entity is visually mindshielded. Does NOT make the entity function as if it was mindshielded.
/// This can be used only for the client (to display mindshield visuals), but it can eventually be used for things like an officer Beepsky who needs to see if crewmembers are mindshielded.
/// </summary>
[ByRefEvent]
public sealed class QueryMindShieldVisualsEvent : EntityEventArgs, IInventoryRelayEvent
{
    public SlotFlags TargetSlots => SlotFlags.All;

    /// <summary>
    /// Wether a mindshield icon is present
    /// </summary>
    public bool IsVisible = false;

    /// <summary>
    /// The mindshield icon to be displayed
    /// </summary>
    public ProtoId<SecurityIconPrototype> MindShieldStatusIcon = "MindShieldIcon";

    /// <summary>
    /// Priority int used to keep trace of MindShieldStatusIcon overwritting.
    /// </summary>
    public int Priority = 0;
}
