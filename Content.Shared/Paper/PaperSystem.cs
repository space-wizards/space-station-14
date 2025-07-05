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
using Robust.Shared.Network;
using Robust.Shared.Player;
using static Content.Shared.Paper.PaperComponent;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Paper;

public sealed class PaperSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly INetManager _net = default!;

    private static readonly ProtoId<TagPrototype> WriteIgnoreStampsTag = "WriteIgnoreStamps";
    private static readonly ProtoId<TagPrototype> WriteTag = "Write";

    private EntityQuery<PaperComponent> _paperQuery;

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

        _paperQuery = GetEntityQuery<PaperComponent>();
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
                    string.Join(", ", entity.Comp.StampedBy.Select(s => Loc.GetString(s.StampedName)));
                args.PushMarkup(
                    Loc.GetString(
                        "paper-component-examine-detail-stamped-by",
                        ("paper", entity),
                        ("stamps", commaSeparated))
                );
            }
        }
    }

    private bool IsWritingTool(EntityUid writingTool)
    {
        return _tagSystem.HasTag(writingTool, WriteTag);
    }

    private bool IsEditable(Entity<PaperComponent> entity, EntityUid writingTool)
    {
        return IsWritingTool(writingTool)
            // only allow editing if there are no stamps or when using a cyberpen
            && (entity.Comp.StampedBy.Count == 0 || _tagSystem.HasTag(writingTool, WriteIgnoreStampsTag));
    }

    private void OnInteractUsing(Entity<PaperComponent> entity, ref InteractUsingEvent args)
    {
        if (IsWritingTool(args.Used))
        {
            if (IsEditable(entity, args.Used))
            {
                var ev = new PaperWriteAttemptEvent(entity.Owner);
                RaiseLocalEvent(args.User, ref ev);
                if (ev.Cancelled)
                {
                    if (ev.FailReason is not null)
                    {
                        var fileWriteMessage = Loc.GetString(ev.FailReason);
                        _popupSystem.PopupClient(fileWriteMessage, entity.Owner, args.User);
                    }

                    args.Handled = true;
                    return;
                }

                var writeEvent = new PaperWriteEvent(args.User, entity);
                RaiseLocalEvent(args.Used, ref writeEvent);

                // Broadcast message to players which have the UI opened,
                // updating the UI with any new contents.
                _uiSystem.OpenUi(entity.Owner, PaperUiKey.Key, args.User);
                UpdateUserInterface(entity);

                if (_net.IsServer)
                {
                    // Send a message only to the player which interacted with the pen.
                    // This will update the UI, enabling edit mode only for that player.
                    var toolNetEnt = EntityManager.GetNetEntity(args.Used);
                    _uiSystem.ServerSendUiMessage(entity.Owner, PaperUiKey.Key, new PaperBeginEditMessage(toolNetEnt), args.User);
                }
            }

            // Handle the event even if we attempted to use a writing tool, but couldn't write on
            // the paper. This prevents crayons from losing durability, etc.
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

            _popupSystem.PopupEntity(stampPaperOtherMessage, args.User, Filter.PvsExcept(args.User, entityManager: EntityManager), true);
            var stampPaperSelfMessage = Loc.GetString("paper-component-action-stamp-paper-self",
                    ("target", args.Target),
                    ("stamp", args.Used));
            _popupSystem.PopupClient(stampPaperSelfMessage, args.User, args.User);

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

    private void OnInputTextMessage(Entity<PaperComponent> entity, ref PaperInputTextMessage args)
    {
        var ev = new PaperWriteAttemptEvent(entity.Owner);
        RaiseLocalEvent(args.Actor, ref ev);
        if (ev.Cancelled)
            return;

        if (args.Text.Length <= entity.Comp.ContentSize && IsEditable(entity, EntityManager.GetEntity(args.EditToolEntity)))
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
        else
        {
            // This block can be hit if the user somehow managed to input more than the maximum content size
            // or if the paper was stamped after they started editing it with a normal, stamp-respecting pen.
            var user = EntityManager.GetEntity(args.User);
            var writeFailedMessage = Loc.GetString("paper-component-action-write-failed");
            _popupSystem.PopupEntity(writeFailedMessage, entity, user);
        }

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
        var dataset = _protoMan.Index(ent.Comp.Dataset);
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
        _uiSystem.SetUiState(entity.Owner, PaperUiKey.Key, new PaperBoundUserInterfaceState(entity.Comp.Content, entity.Comp.StampedBy));
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
