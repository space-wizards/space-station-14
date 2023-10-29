using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Popups;
using Content.Server.UserInterface;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Paper;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using static Content.Shared.Paper.SharedPaperComponent;

namespace Content.Server.Paper
{
    public sealed class PaperSystem : EntitySystem
    {
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly SharedInteractionSystem _interaction = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly TagSystem _tagSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
        [Dependency] private readonly MetaDataSystem _metaSystem = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PaperComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<PaperComponent, BeforeActivatableUIOpenEvent>(BeforeUIOpen);
            SubscribeLocalEvent<PaperComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<PaperComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<PaperComponent, PaperInputTextMessage>(OnInputTextMessage);

            SubscribeLocalEvent<ActivateOnPaperOpenedComponent, PaperWriteEvent>(OnPaperWrite);

            SubscribeLocalEvent<PaperComponent, MapInitEvent>(OnMapInit);
        }

        private void OnMapInit(EntityUid uid, PaperComponent paperComp, MapInitEvent args)
        {
            if (!string.IsNullOrEmpty(paperComp.Content))
            {
                paperComp.Content = Loc.GetString(paperComp.Content);
            }
        }

        private void OnInit(EntityUid uid, PaperComponent paperComp, ComponentInit args)
        {
            paperComp.Mode = PaperAction.Read;
            UpdateUserInterface(uid, paperComp);

            if (TryComp<AppearanceComponent>(uid, out var appearance))
            {
                if (paperComp.Content != "")
                    _appearance.SetData(uid, PaperVisuals.Status, PaperStatus.Written, appearance);

                if (paperComp.StampState != null)
                    _appearance.SetData(uid, PaperVisuals.Stamp, paperComp.StampState, appearance);
            }

        }

        private void BeforeUIOpen(EntityUid uid, PaperComponent paperComp, BeforeActivatableUIOpenEvent args)
        {
            paperComp.Mode = PaperAction.Read;

            if (!TryComp<ActorComponent>(args.User, out var actor))
                return;

            UpdateUserInterface(uid, paperComp, actor.PlayerSession);
        }

        private void OnExamined(EntityUid uid, PaperComponent paperComp, ExaminedEvent args)
        {
            if (!args.IsInDetailsRange)
                return;

            if (paperComp.Content != "")
                args.PushMarkup(
                    Loc.GetString(
                        "paper-component-examine-detail-has-words", ("paper", uid)
                    )
                );

            if (paperComp.StampedBy.Count > 0)
            {
                var commaSeparated = string.Join(", ", paperComp.StampedBy.Select(s => Loc.GetString(s.StampedName)));
                args.PushMarkup(
                    Loc.GetString(
                        "paper-component-examine-detail-stamped-by", ("paper", uid), ("stamps", commaSeparated))
                );
            }
        }

        private void OnInteractUsing(EntityUid uid, PaperComponent paperComp, InteractUsingEvent args)
        {
            if (_tagSystem.HasTag(args.Used, "Write") && paperComp.StampedBy.Count == 0)
            {
                var writeEvent = new PaperWriteEvent(uid, args.User);
                RaiseLocalEvent(args.Used, ref writeEvent);
                if (!TryComp<ActorComponent>(args.User, out var actor))
                    return;

                paperComp.Mode = PaperAction.Write;
                _uiSystem.TryOpen(uid, PaperUiKey.Key, actor.PlayerSession);
                UpdateUserInterface(uid, paperComp, actor.PlayerSession);
                return;
            }

            // If a stamp, attempt to stamp paper
            if (TryComp<StampComponent>(args.Used, out var stampComp) && TryStamp(uid, GetStampInfo(stampComp), stampComp.StampState, paperComp))
            {
                // successfully stamped, play popup
                var stampPaperOtherMessage = Loc.GetString("paper-component-action-stamp-paper-other",
                        ("user", args.User), ("target", args.Target), ("stamp", args.Used));

                _popupSystem.PopupEntity(stampPaperOtherMessage, args.User, Filter.PvsExcept(args.User, entityManager: EntityManager), true);
                var stampPaperSelfMessage = Loc.GetString("paper-component-action-stamp-paper-self",
                        ("target", args.Target), ("stamp", args.Used));
                _popupSystem.PopupEntity(stampPaperSelfMessage, args.User, args.User);

                _audio.PlayPvs(stampComp.Sound, uid);

                UpdateUserInterface(uid, paperComp);
            }
        }

        private StampDisplayInfo GetStampInfo(StampComponent stamp)
        {
            return new StampDisplayInfo {
                StampedName = stamp.StampedName,
                StampedColor = stamp.StampedColor
            };
        }

        private void OnInputTextMessage(EntityUid uid, PaperComponent paperComp, PaperInputTextMessage args)
        {
            if (string.IsNullOrEmpty(args.Text))
                return;

            if (args.Text.Length + paperComp.Content.Length <= paperComp.ContentSize)
                paperComp.Content = args.Text;

            if (TryComp<AppearanceComponent>(uid, out var appearance))
                _appearance.SetData(uid, PaperVisuals.Status, PaperStatus.Written, appearance);

            if (TryComp<MetaDataComponent>(uid, out var meta))
                _metaSystem.SetEntityDescription(uid, "", meta);

            if (args.Session.AttachedEntity != null)
                _adminLogger.Add(LogType.Chat, LogImpact.Low,
                    $"{ToPrettyString(args.Session.AttachedEntity.Value):player} has written on {ToPrettyString(uid):entity} the following text: {args.Text}");

            paperComp.Mode = PaperAction.Read;
            UpdateUserInterface(uid, paperComp);
        }

        private void OnPaperWrite(EntityUid uid, ActivateOnPaperOpenedComponent comp, ref PaperWriteEvent args)
        {
            _interaction.UseInHandInteraction(args.User, uid);
        }

        /// <summary>
        ///     Accepts the name and state to be stamped onto the paper, returns true if successful.
        /// </summary>
        public bool TryStamp(EntityUid uid, StampDisplayInfo stampInfo, string spriteStampState, PaperComponent? paperComp = null)
        {
            if (!Resolve(uid, ref paperComp))
                return false;

            if (!paperComp.StampedBy.Contains(stampInfo))
            {
                paperComp.StampedBy.Add(stampInfo);
                if (paperComp.StampState == null && TryComp<AppearanceComponent>(uid, out var appearance))
                {
                    paperComp.StampState = spriteStampState;
                    // Would be nice to be able to display multiple sprites on the paper
                    // but most of the existing images overlap
                    _appearance.SetData(uid, PaperVisuals.Stamp, paperComp.StampState, appearance);
                }
            }
            return true;
        }

        public void SetContent(EntityUid uid, string content, PaperComponent? paperComp = null)
        {
            if (!Resolve(uid, ref paperComp))
                return;

            paperComp.Content = content + '\n';
            UpdateUserInterface(uid, paperComp);

            if (!TryComp<AppearanceComponent>(uid, out var appearance))
                return;

            var status = string.IsNullOrWhiteSpace(content)
                ? PaperStatus.Blank
                : PaperStatus.Written;

            _appearance.SetData(uid, PaperVisuals.Status, status, appearance);
        }

        public void UpdateUserInterface(EntityUid uid, PaperComponent? paperComp = null, ICommonSession? session = null)
        {
            if (!Resolve(uid, ref paperComp))
                return;

            if (_uiSystem.TryGetUi(uid, PaperUiKey.Key, out var bui))
                _uiSystem.SetUiState(bui, new PaperBoundUserInterfaceState(paperComp.Content, paperComp.StampedBy, paperComp.Mode), session);
        }
    }

    /// <summary>
    /// Event fired when using a pen on paper, opening the UI.
    /// </summary>
    [ByRefEvent]
    public record struct PaperWriteEvent(EntityUid User, EntityUid Paper);
}
