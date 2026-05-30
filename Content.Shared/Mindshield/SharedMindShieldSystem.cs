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
        SubscribeLocalEvent<MindShieldComponent, ImplantRelayEvent<QueryMindShieldStatusEvent>>((e, ref k) => OnStatusQuery(e, ref k.Args));
        SubscribeLocalEvent<MindShieldComponent, InventoryRelayedEvent<QueryMindShieldStatusEvent>>((e, ref k) => OnStatusQuery(e, ref k.Args));
        SubscribeLocalEvent<MindShieldComponent, QueryMindShieldStatusEvent>(OnStatusQuery);

    }

    private void OnStatusQuery(Entity<MindShieldComponent> e, ref QueryMindShieldStatusEvent args)
    {
        args.IsMindshielded = true;
        args.IsVisible = true;
        if (e.Comp.VisualPriority > args.IconPriority)
        {
            args.MindShieldStatusIcon = e.Comp.MindShieldStatusIcon;
        }
    }

    /// <summary>
    /// Retrieves mindshielding data of an entity.
    /// </summary>
    /// <param name="entity">The entity to check the mindshield status of.</param>
    /// <param name="isMindshielded">If the entity has a functional mind shield</param>
    /// <param name="isVisible">Wether the entity shows a mindshield icon on the sec HUD</param>
    /// <param name="statusIcon">Status icon to use for the HUD</param>
    /// <remarks>You should never look for a mindshield component and instead use this function.</remarks>
    public void GetMindshieldStatus(EntityUid entity, out bool isMindshielded, out bool isVisible, out ProtoId<SecurityIconPrototype> statusIcon)
    {
        var ev = new QueryMindShieldStatusEvent();
        RaiseLocalEvent(entity, ref ev);
        isMindshielded = ev.IsMindshielded;
        isVisible = ev.IsVisible;
        statusIcon = ev.MindShieldStatusIcon;
    }
}

/// <summary>
/// Raised in order to query wether an entity is mindshielded, visually or mechanically.
/// </summary>
[ByRefEvent]
public sealed class QueryMindShieldStatusEvent : EntityEventArgs, IInventoryRelayEvent
{
    public SlotFlags TargetSlots => SlotFlags.All;
    /// <summary>
    /// Wether the entity is mindshielded.
    /// </summary>
    public bool IsMindshielded = false;

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
    public int IconPriority = 0;
}
