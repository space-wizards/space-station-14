using System.Linq;
using Content.Shared.Administration.Logs;
using Content.Shared.UserInterface;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Robust.Shared.Player;
using Robust.Shared.Audio.Systems;
using static Content.Shared.Paper.PaperComponent;

namespace Content.Shared.Paper;

public sealed class PaperSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedPaperQuantumSystem _quantum = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PaperComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<PaperComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<PaperComponent, BeforeActivatableUIOpenEvent>(BeforeUIOpen);
        SubscribeLocalEvent<PaperComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<PaperComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<PaperComponent, PaperInputTextMessage>(OnInputTextMessage);

        SubscribeLocalEvent<ActivateOnPaperOpenedComponent, PaperWriteEvent>(OnPaperWrite);
    }

    private void OnMapInit(Entity<PaperComponent> entity, ref MapInitEvent args)
    {
        if (!string.IsNullOrEmpty(entity.Comp.Content))
        {
            SetContent((entity.Owner, entity.Comp), Loc.GetString(entity.Comp.Content));
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
                        "paer-component-examine-detail-has-words",
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

    private void OnInteractUsing(Entity<PaperComponent> entity, ref InteractUsingEvent args)
    {
        // only allow editing if there are no stamps or when using a cyberpen
        if (_tagSystem.HasTag(args.Used, "Write"))
        {
            if (entity.Comp.EditingDisabled)
            {
                var paperEditingDisabledMessage = Loc.GetString("paper-tamper-proof-modified-message");
                _popupSystem.PopupEntity(paperEditingDisabledMessage, entity, args.User);

                args.Handled = true;

                return;
            } 
            var editable = !_quantum.IsEntangled(entity.Owner) && entity.Comp.StampedBy.Count == 0;
            if (editable || _tagSystem.HasTag(args.Used, "WriteIgnoreRestrictions"))
            {
                var writeEvent = new PaperWriteEvent(entity, args.User);
                RaiseLocalEvent(args.Used, ref writeEvent);

                entity.Comp.Mode = PaperAction.Write;
                _uiSystem.OpenUi(entity.Owner, PaperUiKey.Key, args.User);
                UpdateUserInterface(entity);
                args.Handled = true;
            }
            return;
        }

        // If a stamp, attempt to stamp paper
        if (TryComp<StampComponent>(args.Used, out var stampComp))
        {
            var stampInfo = GetStampInfo(stampComp);
            if (TryStamp((entity.Owner, entity.Comp), stampInfo, stampComp.StampState))
            {
                var stampedEvent = new StampedEvent(stampInfo, stampComp.StampState);
                RaiseLocalEvent(entity.Owner, ref stampedEvent);

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
        if (args.Text.Length <= entity.Comp.ContentSize)
        {
            SetContent((entity.Owner, entity.Comp), args.Text);

            _adminLogger.Add(LogType.Chat,
                LogImpact.Low,
                $"{ToPrettyString(args.Actor):player} has written on {ToPrettyString(entity):entity} the following text: {args.Text}");

            _audio.PlayPvs(entity.Comp.Sound, entity);
        }

        entity.Comp.Mode = PaperAction.Read;
        UpdateUserInterface(entity);
    }

    private void OnPaperWrite(Entity<ActivateOnPaperOpenedComponent> entity, ref PaperWriteEvent args)
    {
        _interaction.UseInHandInteraction(args.User, entity);
    }

    /// <summary>
    ///     Fills PaperComponent with existing data.
    /// </summary>
    public void Fill(Entity<PaperComponent?> entity, string content, string? stampState, List<StampDisplayInfo> stampedBy, bool editingDisabled)
    {
        if (!Resolve(entity, ref entity.Comp))
            return;

        SetContent(entity, content);

        // Apply stamps
        if (stampState != null)
        {
            foreach (var stamp in stampedBy)
            {
                TryStamp(entity, stamp, stampState);
            }
        }

        entity.Comp.EditingDisabled = editingDisabled;
    }

    /// <summary>
    ///     Accepts the name and state to be stamped onto the paper, returns true if successful.
    /// </summary>
    public bool TryStamp(Entity<PaperComponent?> entity, StampDisplayInfo stampInfo, string spriteStampState)
    {
        if (!Resolve(entity, ref entity.Comp))
            return false;
        if (entity.Comp.StampedBy.Contains(stampInfo))
            return false;

        entity.Comp.StampedBy.Add(stampInfo);
        if (entity.Comp.StampState == null && TryComp<AppearanceComponent>(entity, out var appearance))
        {
            entity.Comp.StampState = spriteStampState;
            // Would be nice to be able to display multiple sprites on the paper
            // but most of the existing images overlap
            _appearance.SetData(entity, PaperVisuals.Stamp, entity.Comp.StampState, appearance);
        }
        Dirty(entity);
        UpdateUserInterface((entity.Owner, entity.Comp));
        return true;
    }

    public void SetContent(Entity<PaperComponent?> entity, string content)
    {
        if (!Resolve(entity, ref entity.Comp))
            return;
        entity.Comp.Content = content;
        Dirty(entity);
        UpdateUserInterface((entity.Owner, entity.Comp));

        if (!TryComp<AppearanceComponent>(entity, out var appearance))
            return;

        var status = content == ""
            ? PaperStatus.Blank
            : PaperStatus.Written;

        _appearance.SetData(entity, PaperVisuals.Status, status, appearance);
    }

    private void UpdateUserInterface(Entity<PaperComponent> entity)
    {
        _uiSystem.SetUiState(entity.Owner, PaperUiKey.Key, new PaperBoundUserInterfaceState(entity.Comp.Content, entity.Comp.StampedBy, entity.Comp.Mode));
    }
}

/// <summary>
/// Event fired when using a pen on paper, opening the UI.
/// </summary>
[ByRefEvent]
public record struct PaperWriteEvent(EntityUid User, EntityUid Paper);

/// <summary>
/// Event fired when using a rubber stamp on paper.
/// </summary>
[ByRefEvent]
public readonly record struct StampedEvent(StampDisplayInfo StampInfo, string SpriteStampState);
