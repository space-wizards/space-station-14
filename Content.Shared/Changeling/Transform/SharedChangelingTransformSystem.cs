using System.Linq;
using Content.Shared.Actions;
using Content.Shared.Administration.Logs;
using Content.Shared.Chat.TypingIndicator;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Speech.Components;
using Robust.Shared.Serialization;
using Content.Shared.Forensics.Components;
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
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoidAppearanceSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ChangelingTransformComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ChangelingTransformComponent, ChangelingTransformActionEvent>(OnTransformAction);
        SubscribeLocalEvent<ChangelingTransformComponent, ChangelingTransformWindupDoAfterEvent>(OnSuccessfulTransform);
        SubscribeLocalEvent<ChangelingTransformComponent, ChangelingTransformIdentitySelectMessage>(OnTransformSelected);
    }

    private void OnMapInit(Entity<ChangelingTransformComponent> ent, ref MapInitEvent init)
    {
        if(!ent.Comp.ChangelingTransformActionEntity.HasValue)
            _actionsSystem.AddAction(ent, ref ent.Comp.ChangelingTransformActionEntity, ent.Comp.ChangelingTransformAction);

        var userInterfaceComp = EnsureComp<UserInterfaceComponent>(ent);
        _uiSystem.SetUi((ent, userInterfaceComp), TransformUi.Key, new InterfaceData("ChangelingTransformBoundUserInterface"));
    }

    protected virtual void OnTransformAction(Entity<ChangelingTransformComponent> ent,
        ref ChangelingTransformActionEvent args)
    {
        if (!HasComp<UserInterfaceComponent>(ent))
            return;

        if (!TryComp<ChangelingIdentityComponent>(ent, out var userIdentity))
            return;

        Dirty(ent, userIdentity);

        if (!_uiSystem.IsUiOpen(ent.Owner, TransformUi.Key, args.Performer))
        {
            _uiSystem.OpenUi(ent.Owner, TransformUi.Key, args.Performer);

            var x = userIdentity.ConsumedIdentities.Select(x =>
            {
                return new ChangelingIdentityData(GetNetEntity(x),
                    Name(x));
            })
                .ToList();

            _uiSystem.SetUiState(ent.Owner, TransformUi.Key, new ChangelingTransformBoundUserInterfaceState(x));
        }
        else // if the UI is already opened and the command action is done again, transform into the last consumed identity
        {
            TransformPreviousConsumed(ent);
            _uiSystem.CloseUi(ent.Owner, TransformUi.Key, args.Performer);
        }
    }

    private void TransformPreviousConsumed(Entity<ChangelingTransformComponent> ent)
    {
        if (!TryComp<ChangelingIdentityComponent>(ent, out var identity))
            return;

        _popupSystem.PopupPredicted(Loc.GetString("changeling-transform-attempt"), ent, null, PopupType.MediumCaution);
        StartSound(ent, ent.Comp.TransformAttemptNoise);

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager,
            ent,
            ent.Comp.TransformWindup,
            new ChangelingTransformWindupDoAfterEvent(GetNetEntity(identity.LastConsumedEntityUid!.Value)),
            ent,
            used: ent)
        {
            BreakOnMove = true,
            BreakOnWeightlessMove = true,
            DuplicateCondition = DuplicateConditions.None,
        });
    }

    private void OnTransformSelected(Entity<ChangelingTransformComponent> ent,
       ref ChangelingTransformIdentitySelectMessage args)
    {
        _uiSystem.CloseUi(ent.Owner, TransformUi.Key, ent);

        var selectedIdentity = args.TargetIdentity;

        _popupSystem.PopupPredicted(Loc.GetString("changeling-transform-attempt"), ent, null, PopupType.MediumCaution);
        StartSound(ent, ent.Comp.TransformAttemptNoise);
        _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(ent.Owner):player} begun an attempt to transform into \"{Name(GetEntity(selectedIdentity))}\"");

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager,
            ent,
            ent.Comp.TransformWindup,
            new ChangelingTransformWindupDoAfterEvent(selectedIdentity),
            ent,
            used: ent)
        {
            BreakOnMove = true,
            BreakOnWeightlessMove = true,
            DuplicateCondition = DuplicateConditions.None,
        });
    }


    private void OnSuccessfulTransform(Entity<ChangelingTransformComponent> ent,
       ref ChangelingTransformWindupDoAfterEvent args)
    {
        args.Handled = true;

        StopSound(ent);
        if (args.Cancelled)
            return;

        if (!TryComp<VocalComponent>(ent, out var currentVocals)
           || !TryComp<DnaComponent>(ent, out var currentDna)
           || !HasComp<GrammarComponent>(ent))
            return;

        var targetIdentity = GetEntity(args.TargetIdentity);

        if (!TryComp<DnaComponent>(ent, out var targetConsumedDna)
           || !TryComp<HumanoidAppearanceComponent>(targetIdentity, out var targetConsumedHumanoid)
           || !TryComp<VocalComponent>(targetIdentity, out var targetConsumedVocals)
           || !TryComp<SpeechComponent>(targetIdentity, out var targetConsumedSpeech))
            return;

        // Handle species with the ability to wag their tail
        // Why is wagging like this, i can't query the prototype because what if there's things we don't want
        // wagging isn't special, it's just a component, i can't check for special things like this... it's not an emote
        //
        // pukeko: DAMNATION!!!
        if (TryComp<WaggingComponent>(ent, out var waggingComp))
        {
            _actionsSystem.RemoveAction(ent, waggingComp.ActionEntity);
            RemComp<WaggingComponent>(ent);
        }

        if (HasComp<WaggingComponent>(targetIdentity)
            && !HasComp<WaggingComponent>(ent))
        {
            EnsureComp<WaggingComponent>(ent, out _);
        }

        _humanoidAppearanceSystem.CloneAppearance(targetIdentity, args.User);
        TransformBodyEmotes(ent, targetIdentity);

        currentDna.DNA = targetConsumedDna.DNA;

        // Make sure the target Identity has a Typing indicator, if the identity is human or dwarf and never had a mind it'll never have a typingIndicatorComponent
        if (!EnsureComp<TypingIndicatorComponent>(targetIdentity, out _))
            return;

        TryCopyComponents(targetIdentity, ent, MetaData(ent), typeof(VocalComponent), typeof(SpeechComponent), typeof(TypingIndicatorComponent));

        //TODO: While it would be splendid to be able to provide the original owning player who was playing the targetIdentity, it's not exactly feasible to do
        _adminLogger.Add(LogType.Action, LogImpact.High, $"{ToPrettyString(ent.Owner):player}  successfully transformed into \"{Name(targetIdentity)}\"");
        _metaSystem.SetEntityName(ent, Name(targetIdentity), raiseEvents: false);
        _metaSystem.SetEntityDescription(ent, MetaData(targetIdentity).EntityDescription);
        TransformGrammarSet(ent, targetConsumedHumanoid.Gender);

        Dirty(ent);
    }

    protected virtual void TransformBodyEmotes(EntityUid uid, EntityUid target) { }
    public virtual void TransformGrammarSet(EntityUid uid, Gender gender) { }
    protected virtual void StartSound(Entity<ChangelingTransformComponent> ent, SoundSpecifier? sound) { }
    protected virtual void StopSound(Entity<ChangelingTransformComponent> ent) { }
}

public sealed partial class ChangelingTransformActionEvent : InstantActionEvent;

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
