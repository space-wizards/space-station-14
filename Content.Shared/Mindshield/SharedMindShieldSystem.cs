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

public abstract class SharedMindShieldSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        // Mind shield events
        SubscribeLocalEvent<MindShieldComponent, ImplantRelayEvent<QueryMindShieldStatusEvent>>((_, ref k) => k.Args.IsMindshielded = true);
        SubscribeLocalEvent<MindShieldComponent, InventoryRelayedEvent<QueryMindShieldStatusEvent>>((_, ref k) => k.Args.IsMindshielded = true);
        SubscribeLocalEvent<MindShieldComponent, QueryMindShieldStatusEvent>((_, ref k) => k.IsMindshielded = true);
        
        SubscribeLocalEvent<MindShieldComponent, ImplantRelayEvent<QueryMindShieldVisualsEvent>>((a, ref k) => OnQueryMindShieldVisuals(a, ref k.Args));
        SubscribeLocalEvent<MindShieldComponent, InventoryRelayedEvent<QueryMindShieldVisualsEvent>>((a, ref k) => OnQueryMindShieldVisuals(a, ref k.Args));
        SubscribeLocalEvent<MindShieldComponent, QueryMindShieldVisualsEvent>(OnQueryMindShieldVisuals);
        // Fake mind shield events
        

        // TODO
    }

    private void OnQueryMindShieldVisuals(Entity<MindShieldComponent> a, ref QueryMindShieldVisualsEvent k)
    {
        Log.Log(LogLevel.Info, "dope");
        k.IsVisible = true;
    }

    public bool IsMindshielded(EntityUid entity)
    {
        Log.Log(LogLevel.Info, "ms check");
        var ev = new QueryMindShieldStatusEvent();
        RaiseLocalEvent(entity, ref ev);
        return ev.IsMindshielded;
    }

}

/// <summary>
/// Raised in order to query wether an entity is mindshielded
/// </summary>
public sealed class QueryMindShieldStatusEvent : EntityEventArgs, IInventoryRelayEvent
{
    public SlotFlags TargetSlots => SlotFlags.All;
    /// <summary>
    /// Wether the entity is mindshielded.
    /// </summary>
    public bool IsMindshielded = false;
}
/// <summary>
/// Raised in order to query wether an entity is visually mindshielded. Should be raised CLIENT-SIDE only
/// </summary>
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