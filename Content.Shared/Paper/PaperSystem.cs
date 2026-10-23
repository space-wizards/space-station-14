using System.Linq;
using Content.Shared.Administration.Logs;
using Content.Shared.UserInterface;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Random.Helpers;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Robust.Shared.Audio.Systems;
using static Content.Shared.Paper.PaperComponent;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Shared.Verbs;
using Content.Shared.IdentityManagement;
using Robust.Shared.GameStates;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;
using Content.Shared.Inventory;

namespace Content.Shared.Paper;

public abstract partial class PaperSystem : EntitySystem
{
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private SharedPopupSystem _popupSystem = default!;
    [Dependency] private TagSystem _tagSystem = default!;
    [Dependency] private SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private MetaDataSystem _metaSystem = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedDoAfterSystem _doAfterSystem = default!;

    [Dependency] private EntityQuery<PaperComponent> _paperQuery = default!;

    private static readonly ProtoId<TagPrototype> WriteIgnoreStampsTag = "WriteIgnoreStamps";
    private static readonly ProtoId<TagPrototype> WriteTag = "Write";


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PaperComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<PaperComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<PaperComponent, BeforeActivatableUIOpenEvent>(BeforeUIOpen);
        SubscribeLocalEvent<PaperComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<PaperComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<PaperComponent, PaperInputTextMessage>(OnInputTextMessage);
        SubscribeLocalEvent<RandomPaperContentComponent, MapInitEvent>(OnRandomPaperContentMapInit);

        SubscribeLocalEvent<ActivateOnPaperOpenedComponent, PaperWriteEvent>(OnPaperWrite);
    }

    [SubscribeLocalEvent]
    private void OnGetState(Entity<PaperSignerComponent> ent, ref ComponentGetState args)
    {
        args.State = new PaperSignerComponentState(ent.Comp.Signature);
    }

    [SubscribeLocalEvent]
    private void OnHandleState(Entity<PaperSignerComponent> ent, ref ComponentHandleState args)
    {
        if (args.Current is not PaperSignerComponentState state)
            return;

        if (ent.Comp.Signature == state.Signature)
            return;

        ent.Comp.Signature = state.Signature;
        SignatureUpdateUI(ent);
    }

    protected virtual void SignatureUpdateUI(Entity<PaperSignerComponent> ent)
    {
        // implemented on the client
    }

    [SubscribeLocalEvent]
    private void OnSignatureChanged(EntityUid uid, PaperSignerComponent signer, SignatureChangedMessage args)
    {
        var signature = args.Signature.Trim();
        signer.Signature = signature[..Math.Min(signer.MaxSignatureLength, signature.Length)];
        SignatureUpdateUI((uid, signer));
        Dirty(uid, signer);

        // Log label change
        _adminLogger.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(args.Actor):user} set {ToPrettyString(uid):signer} to apply signature \"{signer.Signature}\"");
    }

    [SubscribeLocalEvent]
    private void OnGetUtilityVerb(Entity<PaperSignerComponent> ent, ref GetVerbsEvent<UtilityVerb> args)
    {
        var user = args.User;
        var target = args.Target;

        if (!HasComp<PaperComponent>(target) || !args.CanInteract || !args.CanAccess)
            return;

        if (ent.Comp.DoAfterLength == TimeSpan.Zero)
        {
            var verb = new UtilityVerb()
            {
                Act = () => TrySign(ent, target, user),
                IconEntity = GetNetEntity(ent),
                Text = Loc.GetString("sign-verb-text"),
                Message = Loc.GetString("sign-verb-message")
            };

            args.Verbs.Add(verb);
        }
        else
        {
            var act = () => {
                _popupSystem.PopupEntity(
                    ent.Comp.DoAfterPopupSelf != null ? Loc.GetString(ent.Comp.DoAfterPopupSelf, ("target", target)) : null,
                    ent.Comp.DoAfterPopupOthers != null ? Loc.GetString(ent.Comp.DoAfterPopupOthers, ("user", user), ("target", target)) : null,
                    user,
                    user
                );
                _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager,
                    user,
                    ent.Comp.DoAfterLength,
                    new SignDoAfterEvent(),
                    ent.Owner,
                    target)
                {
                    BreakOnDamage = true,
                    BreakOnMove = true,
                    NeedHand = true,
                });
            };

            var verb = new UtilityVerb()
            {
                Act = act,
                IconEntity = GetNetEntity(ent),
                Text = Loc.GetString("sign-verb-text"),
                Message = Loc.GetString("sign-verb-message")
            };

            args.Verbs.Add(verb);
        }

    }

    private void TrySign(Entity<PaperSignerComponent> ent, EntityUid target, EntityUid user)
    {
        if (!TryComp<PaperComponent>(target, out var paperComp))
            return;

        // do the deed
        var text = Identity.Name(user, EntityManager, user); // todo: replace this with a better system that ignores physical masks but works via identity masks
        var ev = new GetSignatureEvent(user, text);
        var fake = false;

        RaiseLocalEvent(ref ev);

        fake |= ev.Signature != text;
        text = ev.Signature;
        if (ent.Comp.Customize && !ent.Comp.Signature.IsWhiteSpace() && ent.Comp.Signature != "")
        {
            fake |= text != ent.Comp.Signature;
            text = ent.Comp.Signature;
        }

        if (TryStamp((target, paperComp), GetStampInfo(ent.Comp, text, fake), ent.Comp.SignState))
        {
            var signPaperOtherMessage = Loc.GetString("paper-component-action-sign-paper-other",
                ("user", user),
                ("target", target));
            var signPaperSelfMessage = Loc.GetString("paper-component-action-sign-paper-self",
                ("target", target));
            _popupSystem.PopupEntity(signPaperSelfMessage, signPaperOtherMessage, user, user);

            _audio.PlayPredicted(ent.Comp.Sound, ent.Owner, user);

            UpdateUserInterface((target, paperComp));
        }
    }

    [SubscribeLocalEvent]
    private void OnDoAfter(Entity<PaperSignerComponent> entity, ref SignDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Args.Target == null)
            return;

        TrySign(entity, args.Args.Target.Value, args.Args.User);
    }
    private void OnMapInit(Entity<PaperComponent> entity, ref MapInitEvent args)
    {
        if (!string.IsNullOrEmpty(entity.Comp.Content))
        {
            SetContent(entity, Loc.GetString(entity.Comp.Content));
        }
    }

    private void OnInit(Entity<PaperComponent> entity, ref ComponentInit args)
    {
        entity.Comp.Mode = PaperAction.Read;
        UpdateUserInterface(entity);

        if (TryComp<AppearanceComponent>(entity, out var appearance))
        {
            if (entity.Comp.Content != "")
                _appearance.SetData(entity, PaperVisuals.Status, PaperStatus.Written, appearance);

            if (entity.Comp.StampState != null)
                _appearance.SetData(entity, PaperVisuals.Stamp, entity.Comp.StampState, appearance);
        }
    }

    private void BeforeUIOpen(Entity<PaperComponent> entity, ref BeforeActivatableUIOpenEvent args)
    {
        entity.Comp.Mode = PaperAction.Read;
        UpdateUserInterface(entity);
    }

    private void OnExamined(Entity<PaperComponent> entity, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        using (args.PushGroup(nameof(PaperComponent)))
        {
            if (entity.Comp.Content != "")
            {
                args.PushMarkup(
                    Loc.GetString(
                        "paper-component-examine-detail-has-words",
                        ("paper", entity)
                    )
                );
            }

            if (entity.Comp.StampedBy.Count > 0)
            {
                var commaSeparated =
                    string.Join(", ", entity.Comp.StampedBy.Where((k) => !k.Signature).Select(s => Loc.GetString(s.StampedName)));
                if (commaSeparated != "")
                    args.PushMarkup(
                        Loc.GetString(
                            "paper-component-examine-detail-stamped-by",
                            ("paper", entity),
                            ("stamps", commaSeparated))
                    );

                var signersCommaSeparated =
                    string.Join(", ", entity.Comp.StampedBy.Where((k) => k.Signature).Select(s => s.StampedName));
                if (signersCommaSeparated != "")
                    args.PushMarkup(
                        Loc.GetString(
                            "paper-component-examine-detail-signed-by",
                            ("paper", entity),
                            ("signers", signersCommaSeparated))
                    );
            }
        }
    }

    private void OnInteractUsing(Entity<PaperComponent> entity, ref InteractUsingEvent args)
    {
        // only allow editing if there are no stamps or when using a cyberpen
        var editable = entity.Comp.StampedBy.Count == 0 || _tagSystem.HasTag(args.Used, WriteIgnoreStampsTag);
        if (_tagSystem.HasTag(args.Used, WriteTag))
        {
            if (editable)
            {
                if (entity.Comp.EditingDisabled)
                {
                    var paperEditingDisabledMessage = Loc.GetString("paper-tamper-proof-modified-message");
                    _popupSystem.PopupEntity(paperEditingDisabledMessage, entity, args.User);

                    args.Handled = true;
                    return;
                }

                var ev = new PaperWriteAttemptEvent(entity.Owner);
                RaiseLocalEvent(args.User, ref ev);
                if (ev.Cancelled)
                {
                    if (ev.FailReason is not null)
                    {
                        var fileWriteMessage = Loc.GetString(ev.FailReason);
                        _popupSystem.PopupEntity(fileWriteMessage, entity.Owner, args.User);
                    }

                    args.Handled = true;
                    return;
                }

                var writeEvent = new PaperWriteEvent(args.User, entity);
                RaiseLocalEvent(args.Used, ref writeEvent);

                entity.Comp.Mode = PaperAction.Write;
                _uiSystem.OpenUi(entity.Owner, PaperUiKey.Key, args.User);
                UpdateUserInterface(entity);
            }
            args.Handled = true;
            return;
        }

        // If a stamp, attempt to stamp paper
        if (TryComp<StampComponent>(args.Used, out var stampComp) && TryStamp(entity, GetStampInfo(stampComp), stampComp.StampState))
        {
            // successfully stamped, play popup
            var stampPaperOtherMessage = Loc.GetString("paper-component-action-stamp-paper-other",
                    ("user", args.User),
                    ("target", args.Target),
                    ("stamp", args.Used));
            var stampPaperSelfMessage = Loc.GetString("paper-component-action-stamp-paper-self",
                    ("target", args.Target),
                    ("stamp", args.Used));
            _popupSystem.PopupEntity(stampPaperSelfMessage, stampPaperOtherMessage, args.User, args.User);

            _audio.PlayPredicted(stampComp.Sound, entity, args.User);

            UpdateUserInterface(entity);
        }
    }

    private static StampDisplayInfo GetStampInfo(StampComponent stamp)
    {
        return new StampDisplayInfo
        {
            StampedName = stamp.StampedName,
            StampedColor = stamp.StampedColor
        };
    }

    private static StampDisplayInfo GetStampInfo(PaperSignerComponent pen, string signature, bool faked)
    {
        // this is the signature one so its a bit different
        return new StampDisplayInfo
        {
            StampedName = signature,
            StampedColor = pen.SignatureColor,
            Signature = true,
            Border = false,
            Fake = faked
        };
    }

    private void OnInputTextMessage(Entity<PaperComponent> entity, ref PaperInputTextMessage args)
    {
        var ev = new PaperWriteAttemptEvent(entity.Owner);
        RaiseLocalEvent(args.Actor, ref ev);
        if (ev.Cancelled)
            return;

        if (args.Text.Length <= entity.Comp.ContentSize)
        {
            SetContent(entity, args.Text);

            var paperStatus = string.IsNullOrWhiteSpace(args.Text) ? PaperStatus.Blank : PaperStatus.Written;

            if (TryComp<AppearanceComponent>(entity, out var appearance))
                _appearance.SetData(entity, PaperVisuals.Status, paperStatus, appearance);

            if (TryComp(entity, out MetaDataComponent? meta))
                _metaSystem.SetEntityDescription(entity, "", meta);

            _adminLogger.Add(LogType.Chat,
                LogImpact.Low,
                $"{ToPrettyString(args.Actor):player} has written on {ToPrettyString(entity):entity} the following text: {args.Text}");

            _audio.PlayPvs(entity.Comp.Sound, entity);
        }

        entity.Comp.Mode = PaperAction.Read;
        UpdateUserInterface(entity);
    }

    private void OnRandomPaperContentMapInit(Entity<RandomPaperContentComponent> ent, ref MapInitEvent args)
    {
        if (!_paperQuery.TryComp(ent, out var paperComp))
        {
            Log.Warning($"{ToPrettyString(ent)} has a {nameof(RandomPaperContentComponent)} but no {nameof(PaperComponent)}!");
            RemCompDeferred(ent, ent.Comp);
            return;
        }
        var dataset = ProtoMan.Index(ent.Comp.Dataset);
        // Intentionally not using the Pick overload that directly takes a LocalizedDataset,
        // because we want to get multiple attributes from the same pick.
        var pick = _random.Pick(dataset.Values);

        // Name
        _metaSystem.SetEntityName(ent, Loc.GetString(pick));
        // Description
        _metaSystem.SetEntityDescription(ent, Loc.GetString($"{pick}.desc"));
        // Content
        SetContent((ent, paperComp), Loc.GetString($"{pick}.content"));

        // Our work here is done
        RemCompDeferred(ent, ent.Comp);
    }

    private void OnPaperWrite(Entity<ActivateOnPaperOpenedComponent> entity, ref PaperWriteEvent args)
    {
        _interaction.UseInHandInteraction(args.User, entity);
    }

    /// <summary>
    ///     Accepts the name and state to be stamped onto the paper, returns true if successful.
    /// </summary>
    public bool TryStamp(Entity<PaperComponent> entity, StampDisplayInfo stampInfo, string spriteStampState)
    {
        if (!entity.Comp.StampedBy.Contains(stampInfo))
        {
            entity.Comp.StampedBy.Add(stampInfo);
            Dirty(entity);
            if (entity.Comp.StampState == null && TryComp<AppearanceComponent>(entity, out var appearance))
            {
                entity.Comp.StampState = spriteStampState;
                // Would be nice to be able to display multiple sprites on the paper
                // but most of the existing images overlap
                _appearance.SetData(entity, PaperVisuals.Stamp, entity.Comp.StampState, appearance);
            }
        }
        return true;
    }

    /// <summary>
    ///     Copy any stamp information from one piece of paper to another.
    /// </summary>
    public void CopyStamps(Entity<PaperComponent?> source, Entity<PaperComponent?> target)
    {
        if (!Resolve(source, ref source.Comp) || !Resolve(target, ref target.Comp))
            return;

        target.Comp.StampedBy = new List<StampDisplayInfo>(source.Comp.StampedBy);
        target.Comp.StampState = source.Comp.StampState;
        Dirty(target);

        if (TryComp<AppearanceComponent>(target, out var appearance))
        {
            // delete any stamps if the stamp state is null
            _appearance.SetData(target, PaperVisuals.Stamp, target.Comp.StampState ?? "", appearance);
        }
    }

    public void SetContent(EntityUid entity, string content)
    {
        if (!TryComp<PaperComponent>(entity, out var paper))
            return;
        SetContent((entity, paper), content);
    }

    public void SetContent(Entity<PaperComponent> entity, string content)
    {
        entity.Comp.Content = content;
        Dirty(entity);
        UpdateUserInterface(entity);

        if (!TryComp<AppearanceComponent>(entity, out var appearance))
            return;

        var status = string.IsNullOrWhiteSpace(content)
            ? PaperStatus.Blank
            : PaperStatus.Written;

        _appearance.SetData(entity, PaperVisuals.Status, status, appearance);
    }

    private void UpdateUserInterface(Entity<PaperComponent> entity)
    {
        _uiSystem.SetUiState(entity.Owner, PaperUiKey.Key, new PaperBoundUserInterfaceState(entity.Comp.Content, entity.Comp.StampedBy, entity.Comp.Mode));
    }

    /// <summary>
    /// If this paper was signed by the author
    /// </summary>
    /// <param name="entity">paper to check for</param>
    /// <param name="author">author that is searched in the signatures</param>
    /// <param name="ignoreFakes">wether to ignore fake signatures</param>
    public bool IsSignedBy(Entity<PaperComponent> entity, string author, bool ignoreFakes = false)
    {
        return entity.Comp.StampedBy.Count((k) => (!(k.Fake && !ignoreFakes) && k.Signature && k.StampedName.Trim() == author.Trim())) > 0;
    }
}

/// <summary>
/// Event fired when using a pen on paper, opening the UI.
/// </summary>
[ByRefEvent]
public record struct PaperWriteEvent(EntityUid User, EntityUid Paper);

/// <summary>
/// Cancellable event for attempting to write on a piece of paper.
/// </summary>
/// <param name="paper">The paper that the writing will take place on.</param>
[ByRefEvent]
public record struct PaperWriteAttemptEvent(EntityUid Paper, string? FailReason = null, bool Cancelled = false);

/// <summary>
/// Event fired to get an entity's signature. By default, this is its name.
/// </summary>
[ByRefEvent]
public record struct GetSignatureEvent(EntityUid User, string Signature) : IInventoryRelayEvent
{
    public SlotFlags TargetSlots => SlotFlags.All;
}

/// <summary>
/// Used to sign things, after a delay
/// </summary>
[Serializable, NetSerializable]
public sealed partial class SignDoAfterEvent : SimpleDoAfterEvent
{
}
