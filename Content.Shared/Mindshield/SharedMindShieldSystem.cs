using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Implants;
using Content.Shared.Inventory;
using Content.Shared.Mindshield.Components;
using Content.Shared.Mobs;
using Content.Shared.StatusIcon;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Mindshield;

public abstract class SharedMindshieldSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        // Mind shield events
        // Status
        SubscribeLocalEvent<MindshieldComponent, ImplantRelayEvent<QueryMindshieldStatusEvent>>((_, ref k) => k.Args.IsMindshielded = true);
        SubscribeLocalEvent<MindshieldComponent, InventoryRelayedEvent<QueryMindshieldStatusEvent>>((_, ref k) => k.Args.IsMindshielded = true);
        SubscribeLocalEvent<MindshieldComponent, QueryMindshieldStatusEvent>((_, ref k) => k.IsMindshielded = true);
        // Visuals
        SubscribeLocalEvent<MindshieldComponent, ImplantRelayEvent<QueryMindshieldVisualsEvent>>((a, ref k) => OnQueryMindshieldVisuals(a, ref k.Args));
        SubscribeLocalEvent<MindshieldComponent, InventoryRelayedEvent<QueryMindshieldVisualsEvent>>((a, ref k) => OnQueryMindshieldVisuals(a, ref k.Args));
        SubscribeLocalEvent<MindshieldComponent, QueryMindshieldVisualsEvent>(OnQueryMindshieldVisuals);

        // TODO
    }

    private void OnQueryMindshieldVisuals(Entity<MindshieldComponent> a, ref QueryMindshieldVisualsEvent k)
    {
        k.IsVisible = true;
        // Apply the visuals. We check the priority so that things like fake mindshields always get overwritten by real mindshields
        if (a.Comp.VisualPriority > k.Priority)
        {
            k.Priority = a.Comp.VisualPriority;
            k.MindshieldStatusIcon = a.Comp.MindshieldStatusIcon;
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
        var ev = new QueryMindshieldStatusEvent();
        RaiseLocalEvent(entity, ref ev);
        return ev.IsMindshielded;
    }

}

/// <summary>
/// Raised in order to query wether an entity is mindshielded. A mindshield-affecting item/device/component should modify the IsMindshielded flag.
/// </summary>
/// <remarks>
/// Note that this does not make the mindshield icon visible on the security HUD.
/// </remarks>
[ByRefEvent]
public sealed class QueryMindshieldStatusEvent : EntityEventArgs, IInventoryRelayEvent
{
    public SlotFlags TargetSlots => SlotFlags.All;
    /// <summary>
    /// Wether the entity is mindshielded.
    /// </summary>
    public bool IsMindshielded = false;
}
/// <summary>
/// Raised in order to query wether an entity is visually mindshielded. Should be raised CLIENT-SIDE only.
/// If IsVisible is true, this means that a mindshield icon will be visible on the security HUD.
/// </summary>
/// <remarks>
/// This DOES NOT affect actual mindshielding - that is, conversion protection.
/// </remarks>
[ByRefEvent]
public sealed class QueryMindshieldVisualsEvent : EntityEventArgs, IInventoryRelayEvent
{
    public SlotFlags TargetSlots => SlotFlags.All;

    /// <summary>
    /// Wether a mindshield icon is present
    /// </summary>
    public bool IsVisible = false;

    /// <summary>
    /// The mindshield icon to be displayed
    /// </summary>
    public ProtoId<SecurityIconPrototype> MindshieldStatusIcon = "MindshieldIcon";

    /// <summary>
    /// Priority int used to keep trace of MindshieldStatusIcon overwritting.
    /// </summary>
    public int Priority = 0;
}
