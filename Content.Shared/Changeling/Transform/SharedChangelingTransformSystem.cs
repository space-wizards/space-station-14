using System.Linq;
using Content.Shared.Actions;
using Content.Shared.Chat.TypingIndicator;
using Content.Shared.DoAfter;
using Content.Shared.Speech.Components;
using Robust.Shared.Serialization;
using Content.Shared.Forensics;
using Content.Shared.Humanoid;
using Content.Shared.Popups;
using Content.Shared.Speech;
using Content.Shared.Wagging;
using Robust.Shared.Audio;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects.Components.Localization;


namespace Content.Shared.Changeling.Transform;

public abstract partial class SharedChangelingTransformSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoidAppearanceSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedChangelingIdentitySystem _changelingIdentitySystem = default!;
    [Dependency] private readonly SharedTypingIndicatorSystem _typingIndicatorSystem = default!;


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

        var userInterfaceComp = EnsureComp<UserInterfaceComponent>(uid);
        _uiSystem.SetUi((uid, userInterfaceComp), TransformUi.Key, new InterfaceData("ChangelingTransformBoundUserInterface"));

        var identityStorage = EnsureComp<ChangelingIdentityComponent>(uid);
        _changelingIdentitySystem.CloneLingStart(uid, identityStorage);
    }

    protected virtual void OnTransformAction(EntityUid uid,
        ChangelingTransformComponent component,
        ChangelingTransformActionEvent args)
    {
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
                    Name(x));
            })
                .ToList();

            _uiSystem.SetUiState(uid, TransformUi.Key, new ChangelingTransformBoundUserInterfaceState(x));
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
        StartSound(uid, component, component.TransformAttemptNoise);
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
        StartSound(uid, component, component.TransformAttemptNoise);
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

        StopSound(uid, component);
        if (args.Cancelled)
            return;

        if (!TryComp<HumanoidAppearanceComponent>(uid, out var currentAppearance)
           || !TryComp<VocalComponent>(uid, out var currentVocals)
           || !TryComp<SpeechComponent>(uid, out var currentSpeech)
           || !TryComp<DnaComponent>(uid, out var currentDna)
           || !TryComp<TypingIndicatorComponent>(uid, out var currentTypingIndicator)
           || !HasComp<GrammarComponent>(uid))
            return;

        var targetIdentity = GetEntity(args.TargetIdentity);
        if(!TryComp<DnaComponent>(uid, out var targetConsumedDna)
           || !TryComp<HumanoidAppearanceComponent>(targetIdentity, out var targetConsumedHumanoid)
           || !TryComp<VocalComponent>(targetIdentity, out var targetConsumedVocals)
           || !TryComp<SpeechComponent>(targetIdentity, out var targetConsumedSpeech))
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

        TransformBodyEmotes(uid, targetIdentity);

        currentDna.DNA = targetConsumedDna.DNA;
        currentVocals.EmoteSounds = targetConsumedVocals.EmoteSounds;
        currentVocals.Sounds = targetConsumedVocals.Sounds;
        currentVocals.ScreamAction = targetConsumedVocals.ScreamAction;
        currentVocals.ScreamId = targetConsumedVocals.ScreamId;
        currentVocals.Wilhelm = targetConsumedVocals.Wilhelm;
        currentVocals.WilhelmProbability = targetConsumedVocals.WilhelmProbability;

        currentSpeech.SpeechSounds = targetConsumedSpeech.SpeechSounds;
        currentSpeech.SpeechVerb = targetConsumedSpeech.SpeechVerb;
        currentSpeech.SuffixSpeechVerbs = targetConsumedSpeech.SuffixSpeechVerbs;
        currentSpeech.AllowedEmotes = targetConsumedSpeech.AllowedEmotes;
        currentSpeech.AudioParams = targetConsumedSpeech.AudioParams;

        // Make sure the target Identity has a Typing indicator, if the identity is human or dwarf and never had a mind it'll never have a typingIndicatorComponent
        EnsureComp<TypingIndicatorComponent>(targetIdentity, out var targetTypingIndicator);
        SharedTypingIndicatorSystem.Replace(currentTypingIndicator, targetTypingIndicator);

        _metaSystem.SetEntityName(uid, Name(targetIdentity), raiseEvents: false);
        _metaSystem.SetEntityDescription(uid, MetaData(targetIdentity).EntityDescription);
        TransformGrammarSet(uid, targetConsumedHumanoid.Gender);

        Dirty(uid, currentAppearance);
        Dirty(uid, currentVocals);
        Dirty(uid, currentSpeech);
    }

    protected virtual void TransformBodyEmotes(EntityUid uid, EntityUid target) { }
    public virtual void TransformGrammarSet(EntityUid uid, Gender gender) { }
    protected virtual void StartSound(EntityUid uid, ChangelingTransformComponent component, SoundSpecifier? sound) { }
    protected virtual void StopSound(EntityUid uid, ChangelingTransformComponent component) { }
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


    public ChangelingIdentityData(NetEntity identity, string name)
    {
        Identity = identity;
        Name = name;
    }
}
[Serializable, NetSerializable]
public sealed class ChangelingTransformBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly List<ChangelingIdentityData> Identites;
    public ChangelingTransformBoundUserInterfaceState(List<ChangelingIdentityData> identities)
    {
        Identites = identities;
    }
}
[Serializable, NetSerializable]
public enum TransformUi : byte
{
    Key,
}
