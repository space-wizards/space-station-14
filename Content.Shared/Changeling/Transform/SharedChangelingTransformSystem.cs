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
using Content.Shared.Prototypes;
using Content.Shared.Wagging;
using Robust.Shared.Containers;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects.Components.Localization;
using Robust.Shared.Prototypes;


namespace Content.Shared.Changeling.Transform;

public abstract partial class SharedChangelingTransformSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoidAppearanceSystem = default!;
    [Dependency] private readonly NameModifierSystem _nameModifierSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaSystem = default!;
    [Dependency] private readonly GrammarSystem _grammarSystem = default!;
    [Dependency] private readonly SharedIdentitySystem _identitySystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedChangelingIdentitySystem _changelingIdentitySystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ChangelingTransformComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<ChangelingTransformComponent, ChangelingTransformActionEvent>(OnTransformAction);
        SubscribeLocalEvent<ChangelingTransformRadialMessage>(OnRadialmessage);
        SubscribeNetworkEvent<ChangelingIdentityRequest>(OnIdentityRequest);
        SubscribeLocalEvent<ChangelingTransformComponent, ChangelingTransformWindupDoAfterEvent>(OnSuccessfulTransform);
    }

    private void OnInit(EntityUid uid, ChangelingTransformComponent component, MapInitEvent init)
    {
        _actionsSystem.AddAction(uid, ref component.ChangelingTransformActionEntity, component.ChangelingTransformAction);
        var identityStorage = EnsureComp<ChangelingIdentityComponent>(uid);
        if (identityStorage.ConsumedIdentities.Count > 0)
            return;
        _changelingIdentitySystem.CloneToNullspace(uid, identityStorage, uid);
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
            DuplicateCondition = DuplicateConditions.None,
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

    private void OnSuccessfulTransform(EntityUid uid,
        ChangelingTransformComponent component,
        ChangelingTransformWindupDoAfterEvent args)
    {
        args.Handled = true;
        if (args.Cancelled)
            return;
        if(!TryComp<HumanoidAppearanceComponent>(uid, out var currentAppearance))
            return;
        if(!TryComp<VocalComponent>(uid, out var currentVocals))
            return;
        if(!TryComp<DnaComponent>(uid, out var currentDna))
            return;
        if(!HasComp<GrammarComponent>(uid))
            return;
        if(!TryComp<ChangelingIdentityComponent>(args.User, out var identityStorage))
            return;

        var lastConsumed = identityStorage.LastConsumedEntityUid;
        if (lastConsumed == null)
            return;
        if(!TryComp<DnaComponent>(uid, out var lastConsumedDna))
            return;
        if (!TryComp<HumanoidAppearanceComponent>(lastConsumed, out var humanoid))
            return;
        if (!TryComp<VocalComponent>(lastConsumed, out var lastConsumedVocals))
            return;

        //Handle species with the ability to wag their tail
        if (TryComp<WaggingComponent>(uid, out var waggingComp))
        {
            _actionsSystem.RemoveAction(uid, waggingComp.ActionEntity);
            RemComp<WaggingComponent>(uid);
        }
        if (HasComp<WaggingComponent>(lastConsumed)
            && !HasComp<WaggingComponent>(uid))
        {
            EnsureComp<WaggingComponent>(uid, out _);
        }
        _humanoidAppearanceSystem.CloneAppearance((EntityUid)lastConsumed, args.User);
        currentDna.DNA = lastConsumedDna.DNA;
        currentVocals.EmoteSounds = lastConsumedVocals.EmoteSounds;
        currentVocals.Sounds = lastConsumedVocals.Sounds;
        currentVocals.ScreamAction = lastConsumedVocals.ScreamAction;
        currentVocals.ScreamId = lastConsumedVocals.ScreamId;
        currentVocals.Wilhelm = lastConsumedVocals.Wilhelm;
        currentVocals.WilhelmProbability = lastConsumedVocals.WilhelmProbability;
        _metaSystem.SetEntityName(uid, Name((EntityUid)lastConsumed), raiseEvents: false);
        _metaSystem.SetEntityDescription(uid, MetaData((EntityUid)lastConsumed).EntityDescription);
        TransformGrammarSet(uid, humanoid.Gender);

        _entityManager.Dirty(uid, currentAppearance);
        _entityManager.Dirty(uid, currentVocals);
    }

    public virtual void TransformGrammarSet(EntityUid uid, Gender gender) { }

    private void OnIdentityRequest(ChangelingIdentityRequest ev)
    {
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
