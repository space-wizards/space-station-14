using Content.Shared.Clothing;
using Content.Shared.Implants;
using Content.Shared.Inventory;
using Content.Shared.Mindshield.Components;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Mindshield;

public sealed partial class MindShieldSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
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
    }

    [SubscribeLocalEvent]
    private void OnMindshieldUnequip(Entity<MindShieldComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        RefreshMindshieldStatus(args.Wearer);
    }

    [SubscribeLocalEvent]
    private void OnMindshieldEquip(Entity<MindShieldComponent> ent, ref ClothingGotEquippedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        RefreshMindshieldStatus(args.Wearer);
    }

    [SubscribeLocalEvent]
    private void OnMindshieldRemoved(Entity<MindShieldComponent> ent, ref ComponentRemove args)
    {
        RefreshMindshieldStatus(ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnMindshieldImplantRemoved(Entity<MindShieldComponent> ent, ref ImplantRemovedEvent args)
    {
        RefreshMindshieldStatus(args.Implanted);
    }

    [SubscribeLocalEvent]
    private void OnMindshieldImplanted(Entity<MindShieldComponent> ent, ref ImplantImplantedEvent args)
    {
        RefreshMindshieldStatus(args.Implanted);
    }

    [SubscribeLocalEvent]
    private void OnMindshieldMapInit(Entity<MindShieldComponent> ent, ref MapInitEvent args)
    {
        // todo: make it not refresh on implant & clothing items
        RefreshMindshieldStatus(ent.Owner);
    }

    /// <summary>
    /// This function updates <see cref="MindShieldStatusComponent"/>. It should be called when anything makes a modification of its mindshielded-ness.
    /// </summary>
    public void RefreshMindshieldStatus(EntityUid ent)
    {
        var ev = new GetMindShieldStatusEvent();
        RaiseLocalEvent(ent, ref ev);
        var mindshielded = ev.IsMindshielded;
        var visible = ev.IsVisible;

        if (!mindshielded && !visible)
        {
            if (HasComp<MindShieldStatusComponent>(ent))
                RemCompDeferred<MindShieldStatusComponent>(ent);
        }
        else
        {
            EnsureComp<MindShieldStatusComponent>(ent, out var c);
            c.IsMindshielded = mindshielded;
            c.IsVisible = visible;
            Dirty(ent, c);
        }
    }

    [SubscribeLocalEvent]
    private void OnStatusQuery(Entity<MindShieldComponent> e, ref GetMindShieldStatusEvent args)
    {
        args.IsMindshielded = true;
        args.IsVisible = true;
    }

    /// <summary>
    /// Retrieves mindshielding data of an entity. Works via <see cref="MindShieldStatusComponent"/>, and so requires proper dirtying on the part of mindshield providers.
    /// </summary>
    /// <param name="entity">The entity to check the mindshield status of.</param>
    /// <param name="isMindshielded">If the entity has a functional mind shield</param>
    /// <param name="isVisible">Wether the entity shows a mindshield icon on the sec HUD</param>
    /// <remarks>You should never look for a mindshield component and instead use this function.</remarks>
    public void GetMindshieldStatus(EntityUid entity, out bool isMindshielded, out bool isVisible)
    {
        if (TryComp<MindShieldStatusComponent>(entity, out var comp))
        {
            isMindshielded = comp.IsMindshielded;
            isVisible = comp.IsVisible;
        }
        else
        {
            isMindshielded = isVisible = false;
        }
    }
}

/// <summary>
/// Raised in order to get whether an entity is mindshielded visually, mechanically or both.
/// </summary>
[ByRefEvent, GenericEvent]
public sealed class GetMindShieldStatusEvent : EntityEventArgs, IInventoryRelayEvent, IInventoryRelayAfterImplantEvent
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
