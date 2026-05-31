using Content.Shared.Implants;
using Content.Shared.Inventory;
using Content.Shared.Mindshield.Components;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;

namespace Content.Shared.Mindshield;

public abstract class SharedMindShieldSystem : EntitySystem
{
    /// <summary>
    /// Status icon displayed in the sec HUD.
    /// </summary>
    public static ProtoId<SecurityIconPrototype> StatusIcon = "MindShieldIcon";

    public override void Initialize()
    {
        base.Initialize();

        // Mind shield status events
        SubscribeLocalEvent<MindShieldComponent, ImplantRelayEvent<GetMindShieldStatusEvent>>((e, ref k) => OnStatusQuery(e, ref k.Args));
        SubscribeLocalEvent<MindShieldComponent, InventoryRelayedEvent<GetMindShieldStatusEvent>>((e, ref k) => OnStatusQuery(e, ref k.Args));
        SubscribeLocalEvent<MindShieldComponent, GetMindShieldStatusEvent>(OnStatusQuery);
    }

    private void OnStatusQuery(Entity<MindShieldComponent> e, ref GetMindShieldStatusEvent args)
    {
        args.IsMindshielded = true;
        args.IsVisible = true;
    }

    /// <summary>
    /// Retrieves mindshielding data of an entity.
    /// </summary>
    /// <param name="entity">The entity to check the mindshield status of.</param>
    /// <param name="isMindshielded">If the entity has a functional mind shield</param>
    /// <param name="isVisible">Wether the entity shows a mindshield icon on the sec HUD</param>
    /// <remarks>You should never look for a mindshield component and instead use this function.</remarks>
    public void GetMindshieldStatus(EntityUid entity, out bool isMindshielded, out bool isVisible)
    {
        var ev = new GetMindShieldStatusEvent();
        RaiseLocalEvent(entity, ref ev);
        isMindshielded = ev.IsMindshielded;
        isVisible = ev.IsVisible;
    }
}

/// <summary>
/// Raised in order to get whether an entity is mindshielded visually, mechanically or both.
/// </summary>
[ByRefEvent]
public sealed class GetMindShieldStatusEvent : EntityEventArgs, IInventoryRelayEvent
{
    public SlotFlags TargetSlots => SlotFlags.WITHOUT_POCKET;
    /// <summary>
    /// Whether the entity is mindshielded.
    /// </summary>
    public bool IsMindshielded;

    /// <summary>
    /// Whether a mindshield icon is present
    /// </summary>
    public bool IsVisible;
}
