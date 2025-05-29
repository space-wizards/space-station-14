using System.Linq;
using Content.Shared.Access.Components;
using Content.Shared.Actions;
using Content.Shared.Clothing.Components;
using Content.Shared.Contraband;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Light;
using Content.Shared.Light.Components;
using Content.Shared.Prototypes;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using Microsoft.Extensions.ObjectPool;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Utility;

namespace Content.Shared.Clothing.EntitySystems;

public abstract class SharedChameleonClothingSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly ClothingSystem _clothingSystem = default!;
    [Dependency] private readonly ContrabandSystem _contraband = default!;
    [Dependency] private readonly SharedHandheldLightSystem _handheldLight = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedItemSystem _itemSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly ISerializationManager _serialization = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] protected readonly SharedUserInterfaceSystem UI = default!;
    [Dependency] protected internal readonly IEntityManager EntMan = default!;

    private static readonly ProtoId<TagPrototype> WhitelistChameleonTag = "WhitelistChameleon";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ChameleonClothingComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<ChameleonClothingComponent, GotUnequippedEvent>(OnGotUnequipped);
        SubscribeLocalEvent<ChameleonClothingComponent, GetVerbsEvent<InteractionVerb>>(OnVerb);
    }

    private void OnGotEquipped(EntityUid uid, ChameleonClothingComponent component, GotEquippedEvent args)
    {
        component.User = args.Equipee;
    }

    private void OnGotUnequipped(EntityUid uid, ChameleonClothingComponent component, GotUnequippedEvent args)
    {
        component.User = null;
    }

    // Updates chameleon visuals and meta information.
    // This function is called on a server after user selected new outfit.
    // And after that on a client after state was updated.
    // This 100% makes sure that server and client have exactly same data.
    protected void UpdateVisuals(EntityUid uid, ChameleonClothingComponent component)
    {
        if (string.IsNullOrEmpty(component.Default) ||
            !_proto.TryIndex(component.Default, out EntityPrototype? proto))
            return;

        // copy name and description, unless its an ID card
        if (!HasComp<IdCardComponent>(uid))
        {
            var meta = MetaData(uid);
            _metaData.SetEntityName(uid, proto.Name, meta);
            _metaData.SetEntityDescription(uid, proto.Description, meta);
        }

        EnsureCompAndCopyDetails<HandheldLightComponent>(uid, proto);
        EnsureCompAndCopyDetails<AppearanceComponent>(uid, proto);

        // clothing sprite logic
        if (TryComp(uid, out ClothingComponent? clothing) &&
            proto.TryGetComponent("Clothing", out ClothingComponent? otherClothing))
        {
            _clothingSystem.CopyVisuals(uid, otherClothing, clothing);
        }

        EnsureCompAndCopyDetails<ItemComponent>(uid, proto);
        EnsureCompAndCopyDetails<ContrabandComponent>(uid, proto);

        // world sprite icon
        UpdateSprite(uid, proto);
    }

    private void OnVerb(Entity<ChameleonClothingComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || ent.Comp.User != args.User)
            return;

        // Can't pass args from a ref event inside of lambdas
        var user = args.User;

        args.Verbs.Add(new InteractionVerb()
        {
            Text = Loc.GetString("chameleon-component-verb-text"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/settings.svg.192dpi.png")),
            Act = () => UI.TryToggleUi(ent.Owner, ChameleonUiKey.Key, user)
        });
    }

    protected virtual void UpdateSprite(EntityUid uid, EntityPrototype proto) { }

    /// <summary>
    ///     Check if this entity prototype is valid target for chameleon item.
    /// </summary>
    public bool IsValidTarget(EntityPrototype proto, SlotFlags chameleonSlot = SlotFlags.NONE, HashSet<ProtoId<TagPrototype>>? requiredTags = null)
    {
        // check if entity is valid
        if (proto.Abstract || proto.HideSpawnMenu)
            return false;

        // check if it is marked as valid chameleon target
        if (!proto.TryGetComponent(out TagComponent? tag, Factory) || !_tag.HasTag(tag, WhitelistChameleonTag))
            return false;

        if (requiredTags != null && requiredTags.Any() && requiredTags.All(requiredTag => !_tag.HasTag(tag, requiredTag)))
            return false;

        // check if it's valid clothing
        if (!proto.TryGetComponent("Clothing", out ClothingComponent? clothing))
            return false;
        if (!clothing.Slots.HasFlag(chameleonSlot))
            return false;

        return true;
    }

    protected void EnsureCompAndCopyDetails<T>(EntityUid uid, EntityPrototype proto, Action<T, T?>? afterAddAction = null) where T : IComponent, new()
    {
        // if the new proto does not have the component, remove it from the entity
        if (TryComp<T>(uid, out var previousComponent) && !proto.HasComponent<T>(EntMan.ComponentFactory))
            RemComp<T>(uid);
        // if the new proto has the component, but the entity does not, add it to the entity
        else if (!HasComp<T>(uid) && proto.TryGetComponent<T>(out var protoComonent, EntMan.ComponentFactory))
        {
            var newComponent = EntMan.ComponentFactory.GetComponent<T>();
            _serialization.CopyTo(protoComonent, ref newComponent);
            AddComp(uid, newComponent);
        }
        // if the new proto has the component and the entity does too, copy the data from the proto to the entity
        else if (TryComp<T>(uid, out var currentComponent) && proto.TryGetComponent<T>(out var otherComponent, EntMan.ComponentFactory))
            _serialization.CopyTo(otherComponent, ref currentComponent);


        if (TryComp(uid, out T? component))
        {
            if (component.NetSyncEnabled)
                Dirty(uid, component);

            if (afterAddAction != null)
                afterAddAction(component, previousComponent);
        }
    }
}
