using Content.Shared.Actions;
using Content.Shared.Changeling.Devour;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.DoAfter;
using Content.Shared.Mind;
using Content.Shared.Speech.Components;
using Robust.Shared.Network;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Content.Shared.Forensics;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.IdentityManagement.Components;
using Content.Shared.NameModifier.Components;
using Content.Shared.NameModifier.EntitySystems;
using Content.Shared.Popups;
using Robust.Client.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects.Components.Localization;


namespace Content.Shared.Changeling.Transform;

public abstract partial class SharedChangelingTransformSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly NameModifierSystem _nameModifierSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaSystem = default!;
    [Dependency] private readonly GrammarSystem _grammarSystem = default!;
    [Dependency] private readonly SharedIdentitySystem _identitySystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ChangelingTransformComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<ChangelingTransformComponent, ChangelingTransformActionEvent>(OnTransformAction);
        SubscribeLocalEvent<ChangelingTransformRadialMessage>(OnRadialmessage);
        SubscribeNetworkEvent<ChangelingIdentityRequest>(OnIdentityRequest);
    }

    private void OnInit(EntityUid uid, ChangelingTransformComponent component, MapInitEvent init)
    {
        _actionsSystem.AddAction(uid, ref component.ChangelingTransformActionEntity, component.ChangelingTransformAction);


        component.ChangelingIdentities = EnsureComp<ChangelingIdentityComponent>(uid);
        if (component.ChangelingIdentities.OriginalIdentityComponent == null &&
            TryComp<HumanoidAppearanceComponent>(uid, out var appearance) &&
            TryComp<VocalComponent>(uid, out var vocals) &&
            TryComp<DnaComponent>(uid, out var dna))
        {
            StoredIdentityComponent ling = new()
            {
                IdentityName = Name(uid),
                IdentityDna = dna,
                IdentityAppearance = appearance,
                IdentityVocals = vocals,
                IdentityEntityPrototype = Prototype(uid),
            };
            component.ChangelingIdentities.Identities?.Add(ling);
            component.ChangelingIdentities.OriginalIdentityComponent = ling;
            Dirty(uid, component);
        }
    }

    private void OnTransformAction(EntityUid uid,
        ChangelingTransformComponent component,
        ChangelingTransformActionEvent args)
    {
        var user = args.Performer;
        _popupSystem.PopupPredicted(Loc.GetString("changeling-transform-attempt"), user, null, PopupType.MediumCaution);

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager,
            uid,
            component.TransformWindup,
            new ChangelingTransformWindupDoAfterEvent(),
            uid,
            used: uid)
        {
            BreakOnMove = true,
            BreakOnWeightlessMove = true,

        });


        // if (TryComp<UserInterfaceComponent>(component.ChangelingTransformActionEntity, out var userInterface))
        // {
        //     var isOpen = _uiSystem.IsUiOpen(userInterface.Owner, TransformUi.Key, user);
        //     if (isOpen)
        //     {
        //         _uiSystem.CloseUi(userInterface.Owner, TransformUi.Key, user);
        //     }
        //     else
        //     {
        //         _uiSystem.OpenUi(userInterface.Owner, TransformUi.Key, user);
        //     }
        // }
    }

    private void OnIdentityRequest(ChangelingIdentityRequest ev)
    {
        _mindSystem.TryGetMind(ev.ChannelId, out var mindId, out var mind);

        TryComp<ChangelingIdentityComponent>(mind?.OwnedEntity, out var identity);
        if (identity?.Identities != null)
        {
            var identityList = new List<ChangelingIdentityMessageComposite>();
            foreach (var ident in identity?.Identities!)
            {
                var piece = new ChangelingIdentityMessageComposite()
                {
                    Dna = ident.IdentityDna?.DNA,
                    Sprite = new SpriteSpecifier.Rsi(new ResPath("/Textures/Interface/Actions/changeling.rsi"),
                        "transform"),
                    Tooltip = ident.IdentityDna?.DNA,
                };
                identityList.Add(piece);
            }
            RaiseNetworkEvent(new ChangelingIdentityResponse(ev.ChannelId, identityList));
        }
    }


    private void OnRadialmessage(ChangelingTransformRadialMessage ev)
    {
        if (!TryGetEntity(ev.Entity, out var target))
            return;

        ev.Event.User = ev.Actor;
        RaiseLocalEvent(target.Value, (object) ev.Event);
    }

    private void OnGetIdentities(Entity<ChangelingTransformComponent> component, ref GetChangelingIdentitiesRadialEvent ev)
    {
        TryComp<ChangelingIdentityComponent>(component.Owner, out var identites);
        var o = component.Comp.ChangelingIdentities?.Identities;
        if (component.Comp.ChangelingIdentities?.Identities != null)
        {
            foreach (var identity in component.Comp.ChangelingIdentities.Identities)
            {
                ev.Actions.Add(new()
                    {
                        Sprite = new SpriteSpecifier.Rsi(new ResPath("/Textures/Interface/Actions/changeling.rsi"), "transform"),
                        Tooltip = identity.IdentityDna?.DNA,
                    }
                );
            }
        }
    }
}


[Serializable, NetSerializable]
public sealed class ChangelingIdentityRequest: EntityEventArgs
{
    public NetUserId ChannelId { get; }
    public ChangelingIdentityRequest(NetUserId channelId)
    {
        ChannelId = channelId;
    }
}

[Serializable, NetSerializable]
public sealed class ChangelingIdentityMessageComposite
{
    public String? Dna;
    public SpriteSpecifier? Sprite;
    public String? Tooltip;
}
[Serializable, NetSerializable]
public sealed class ChangelingIdentityResponse : EntityEventArgs
{
    public NetUserId ChannelId { get; }
    public List<ChangelingIdentityMessageComposite> Identities { get; }

    public ChangelingIdentityResponse(NetUserId channelId, List<ChangelingIdentityMessageComposite> identities)
    {
        ChannelId = channelId;
        Identities = identities;
    }

}

public sealed partial class ChangelingTransformActionEvent : InstantActionEvent
{

}

[Serializable, NetSerializable]
public sealed class ChangelingTransformRadialMessage : BoundUserInterfaceMessage
{
    public ChangelingTransformTargetAction Event = default!;
}
public sealed class ChangelingTransformRadialOption : ChangelingTransformTargetAction
{
    public SpriteSpecifier? Sprite;

    public string? Tooltip;

    //public ChangelingTransformTargetAction Event = default!;
}

[Serializable, NetSerializable]
public abstract class ChangelingTransformTargetAction
{
    [field: NonSerialized]
    public EntityUid User { get; set; }
}

[ByRefEvent]
public record struct GetChangelingIdentitiesRadialEvent()
{
    public List<ChangelingTransformRadialOption> Actions = new();
}

[Serializable, NetSerializable]
public sealed partial class ChangelingTransformWindupDoAfterEvent : SimpleDoAfterEvent
{

}

[Serializable, NetSerializable]
public enum TransformUi : byte
{
    Key,
}
