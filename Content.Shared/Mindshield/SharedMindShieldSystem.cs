using Content.Shared.Clothing;
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
        SubscribeLocalEvent<MindShieldComponent, MapInitEvent>(OnMindshieldMapInit);
        // these five events are to manage the mindshield's dirtying
        SubscribeLocalEvent<MindShieldComponent, ImplantImplantedEvent>(OnMindshieldImplanted);
        SubscribeLocalEvent<MindShieldComponent, ImplantRemovedEvent>(OnMindshieldImplantRemoved);
        SubscribeLocalEvent<MindShieldComponent, ComponentRemove>(OnMindshieldRemoved);
        SubscribeLocalEvent<MindShieldComponent, ClothingGotEquippedEvent>(OnMindshieldEquip);
        SubscribeLocalEvent<MindShieldComponent, ClothingGotUnequippedEvent>(OnMindshieldUnequip);
    }

    private void OnMindshieldUnequip(Entity<MindShieldComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        RefreshMindshieldStatus(args.Wearer);
    }

    private void OnMindshieldEquip(Entity<MindShieldComponent> ent, ref ClothingGotEquippedEvent args)
    {
        RefreshMindshieldStatus(args.Wearer);
    }

    private void OnMindshieldRemoved(Entity<MindShieldComponent> ent, ref ComponentRemove args)
    {
        RefreshMindshieldStatus(ent.Owner);
    }

    private void OnMindshieldImplantRemoved(Entity<MindShieldComponent> ent, ref ImplantRemovedEvent args)
    {
        RefreshMindshieldStatus(args.Implanted);
    }

    private void OnMindshieldImplanted(Entity<MindShieldComponent> ent, ref ImplantImplantedEvent args)
    {
        RefreshMindshieldStatus(args.Implanted);
    }

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
        GetMindshieldStatusInner(ent, out var mindshielded, out var visible);
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

    // Used to refresh the cache
    private void GetMindshieldStatusInner(EntityUid entity, out bool isMindshielded, out bool isVisible)
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
