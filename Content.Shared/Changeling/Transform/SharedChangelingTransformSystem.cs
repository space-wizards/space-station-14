using System.Linq;
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
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
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
using Robust.Shared.Map;
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
        SubscribeLocalEvent<ChangelingTransformComponent, ChangelingTransformWindupDoAfterEvent>(OnSuccessfulTransform);
        SubscribeLocalEvent<ChangelingTransformComponent, ChangelingTransformIdentitySelectMessage>(OnTransformSelected);
    }

    private void OnInit(EntityUid uid, ChangelingTransformComponent component, MapInitEvent init)
    {
        if(!component.ChangelingTransformActionEntity.HasValue)
            _actionsSystem.AddAction(uid, ref component.ChangelingTransformActionEntity, component.ChangelingTransformAction);
        TryComp<UserInterfaceComponent>(component.ChangelingTransformActionEntity, out var comp);

        var uiE = EnsureComp<UserInterfaceComponent>(uid);
        _uiSystem.SetUi((uid, uiE), TransformUi.Key, new InterfaceData("ChangelingTransformBoundUserInterface"));

        var identityStorage = EnsureComp<ChangelingIdentityComponent>(uid);
        _changelingIdentitySystem.CloneLingStart(uid, identityStorage);
    }

    protected virtual void OnTransformAction(EntityUid uid,
        ChangelingTransformComponent component,
        ChangelingTransformActionEvent args)
    {
        var user = args.Performer;

        if (!TryComp<UserInterfaceComponent>(uid, out var userInterface))
            return;
        if(!TryComp<ChangelingIdentityComponent>(uid, out var userIdentity))
            return;
        Dirty(uid, userIdentity);

        if (!_uiSystem.IsUiOpen(uid, TransformUi.Key, args.Performer))
        {
            _uiSystem.OpenUi(uid, TransformUi.Key, args.Performer);

            var x = userIdentity.ConsumedIdentities.Select(x =>
            {
                return new ChangelingIdentityData(GetNetEntity(x),
                    Name(x),
                    MetaData(x).EntityDescription);
            })
                .ToList();

            _uiSystem.SetUiState(uid, TransformUi.Key, new ChangelingTransformBoundUserInterfaceState(x, _changelingIdentitySystem.PausedMapId));
        }
        else // if the UI is already opened and the command action is done again, transform into the last consumed identity
        {
            TransformPreviousConsumed(uid, component);
            _uiSystem.CloseUi(uid, TransformUi.Key, args.Performer);
        }
    }

    private void TransformPreviousConsumed(EntityUid uid, ChangelingTransformComponent component)
    {
        if(!TryComp<ChangelingIdentityComponent>(uid, out var identity))
            return;
        _popupSystem.PopupPredicted(Loc.GetString("changeling-transform-attempt"), uid, null, PopupType.MediumCaution);
        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager,
            uid,
            component.TransformWindup,
            new ChangelingTransformWindupDoAfterEvent(GetNetEntity(identity.LastConsumedEntityUid!.Value)),
            uid,
            used: uid)
        {
            BreakOnMove = true,
            BreakOnWeightlessMove = true,
            DuplicateCondition = DuplicateConditions.None,
        });
    }

    private void OnTransformSelected(EntityUid uid,
        ChangelingTransformComponent component,
        ChangelingTransformIdentitySelectMessage args)
    {
        _uiSystem.CloseUi(uid, TransformUi.Key, uid);
        var selectedIdentity = args.TargetIdentity;
        _popupSystem.PopupPredicted(Loc.GetString("changeling-transform-attempt"), uid, null, PopupType.MediumCaution);
        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager,
            uid,
            component.TransformWindup,
            new ChangelingTransformWindupDoAfterEvent(selectedIdentity),
            uid,
            used: uid)
        {
            BreakOnMove = true,
            BreakOnWeightlessMove = true,
            DuplicateCondition = DuplicateConditions.None,
        });
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

        var targetIdentity = GetEntity(args.TargetIdentity);
        if(!TryComp<DnaComponent>(uid, out var lastConsumedDna))
            return;
        if (!TryComp<HumanoidAppearanceComponent>(targetIdentity, out var humanoid))
            return;
        if (!TryComp<VocalComponent>(targetIdentity, out var lastConsumedVocals))
            return;

        //Handle species with the ability to wag their tail
        if (TryComp<WaggingComponent>(uid, out var waggingComp))
        {
            _actionsSystem.RemoveAction(uid, waggingComp.ActionEntity);
            RemComp<WaggingComponent>(uid);
        }
        if (HasComp<WaggingComponent>(targetIdentity)
            && !HasComp<WaggingComponent>(uid))
        {
            EnsureComp<WaggingComponent>(uid, out _);
        }

        _humanoidAppearanceSystem.CloneAppearance(targetIdentity, args.User);

        currentDna.DNA = lastConsumedDna.DNA;
        currentVocals.EmoteSounds = lastConsumedVocals.EmoteSounds;
        currentVocals.Sounds = lastConsumedVocals.Sounds;
        currentVocals.ScreamAction = lastConsumedVocals.ScreamAction;
        currentVocals.ScreamId = lastConsumedVocals.ScreamId;
        currentVocals.Wilhelm = lastConsumedVocals.Wilhelm;
        currentVocals.WilhelmProbability = lastConsumedVocals.WilhelmProbability;

        _metaSystem.SetEntityName(uid, Name(targetIdentity), raiseEvents: false);
        _metaSystem.SetEntityDescription(uid, MetaData(targetIdentity).EntityDescription);
        TransformGrammarSet(uid, humanoid.Gender);

        _entityManager.Dirty(uid, currentAppearance);
        _entityManager.Dirty(uid, currentVocals);
    }

    protected virtual void TransformGrammarSet(EntityUid uid, Gender gender) { }

}


public sealed partial class ChangelingTransformActionEvent : InstantActionEvent
{

}
[Serializable, NetSerializable]
public sealed partial class ChangelingTransformWindupDoAfterEvent : SimpleDoAfterEvent
{
    public NetEntity TargetIdentity;

    public ChangelingTransformWindupDoAfterEvent(NetEntity targetIdentity)
    {
        TargetIdentity = targetIdentity;
    }
}

[Serializable, NetSerializable]
public sealed class ChangelingTransformIdentitySelectMessage : BoundUserInterfaceMessage
{
    public readonly NetEntity TargetIdentity;

    public ChangelingTransformIdentitySelectMessage(NetEntity targetIdentity)
    {
        TargetIdentity = targetIdentity;
    }
}

[Serializable, NetSerializable]
public sealed class ChangelingIdentityData
{
    public readonly NetEntity Identity;
    public string Name;
    public string Description;
    public ProtoId<SpeciesPrototype> Species;


    public ChangelingIdentityData(NetEntity identity, string name, string description)
    {
        Identity = identity;
        Name = name;
        Description = description;
    }
}
[Serializable, NetSerializable]
public sealed class ChangelingTransformBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly List<ChangelingIdentityData> Identites;
    public readonly MapId PausedMapId;

    public ChangelingTransformBoundUserInterfaceState(List<ChangelingIdentityData> identities, MapId pausedMapId)
    {
        Identites = identities;
        PausedMapId = pausedMapId;
    }
}
[Serializable, NetSerializable]
public enum TransformUi : byte
{
    Key,
}
