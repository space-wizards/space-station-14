using Content.Server.UserInterface;
using Content.Shared.Paper;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Content.Server.Popups;
using Robust.Shared.Player;

using static Content.Shared.Paper.SharedPaperComponent;

namespace Content.Server.Paper
{
    public sealed class PaperSystem : EntitySystem
    {
        [Dependency] private readonly TagSystem _tagSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PaperComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<PaperComponent, BeforeActivatableUIOpenEvent>(AfterUIOpen);
            SubscribeLocalEvent<PaperComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<PaperComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<PaperComponent, PaperInputTextMessage>(OnInputTextMessage);
        }

        private void OnInit(EntityUid uid, PaperComponent paperComp, ComponentInit args)
        {
            paperComp.Mode = PaperAction.Read;
            UpdateUserInterface(uid, paperComp);
        }

        private void AfterUIOpen(EntityUid uid, PaperComponent paperComp, BeforeActivatableUIOpenEvent args)
        {
            paperComp.Mode = SharedPaperComponent.PaperAction.Read;
            UpdateUserInterface(uid, paperComp);
        }

        private void OnExamined(EntityUid uid, PaperComponent paperComp, ExaminedEvent args)
        {
            if (!args.IsInDetailsRange)
                return;

            if (paperComp.Content != "")
                args.Message.AddMarkup(
                    Loc.GetString(
                        "paper-component-examine-detail-has-words"
                    )
                );

            if (paperComp.StampedBy.Count > 0)
            {
                args.Message.PushNewline();
                string commaSeparated = string.Join(", ", paperComp.StampedBy);
                args.Message.AddMarkup(
                    Loc.GetString(
                        "paper-component-examine-detail-stamped-by", ("stamps", commaSeparated))
                );
            }
        }

        private void OnInteractUsing(EntityUid uid, PaperComponent paperComp, InteractUsingEvent args)
        {
            if (_tagSystem.HasTag(args.Used, "Write"))
            {
                if (!TryComp<ActorComponent>(args.User, out var actor))
                return;

                paperComp.Mode = PaperAction.Write;
                UpdateUserInterface(uid, paperComp);
                paperComp.UserInterface?.Open(actor.PlayerSession);
                return;
            }

            if (TryComp<StampComponent>(args.Used, out var stampComp))
            {
                if (!paperComp.StampedBy.Contains(stampComp.StampedName))
                {
                    paperComp.StampedBy.Add(stampComp.StampedName);

                    // this is the first stamp, set appearance
                    if (paperComp.StampedBy.Count == 1)
                        if (TryComp<AppearanceComponent>(uid, out var appearance))
                            appearance.SetData(PaperVisuals.Stamped, true);
                }

                var stampPaperOtherMessage = Loc.GetString("paper-component-action-stamp-paper-other", ("user", args.User),("target", args.Target),("stamp", args.Used));
                    _popupSystem.PopupEntity(stampPaperOtherMessage, args.User, Filter.Pvs(args.User).RemoveWhereAttachedEntity(puid => puid == args.User));
                var stampPaperSelfMessage = Loc.GetString("paper-component-action-stamp-paper-self", ("target", args.Target),("stamp", args.Used));
                    _popupSystem.PopupEntity(stampPaperSelfMessage, args.User, Filter.Entities(args.User));
                return;
            }
        }

        private void OnInputTextMessage(EntityUid uid, PaperComponent paperComp, PaperInputTextMessage args)
        {
            if (string.IsNullOrEmpty(args.Text))
                return;

            if (args.Text.Length + paperComp.Content.Length <= paperComp.ContentSize)
                paperComp.Content += args.Text + '\n';

            if (TryComp<AppearanceComponent>(uid, out var appearance))
                appearance.SetData(PaperVisuals.Status, PaperStatus.Written);

            if (TryComp<MetaDataComponent>(uid, out var meta))
                meta.EntityDescription = "";

            UpdateUserInterface(uid, paperComp);
        }

        public void SetContent(EntityUid uid, string content, PaperComponent? paperComp)
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

            appearance.SetData(PaperVisuals.Status, status);
        }

        public void UpdateUserInterface(EntityUid uid, PaperComponent? paperComp)
        {
            if (!Resolve(uid, ref paperComp))
                return;

            paperComp.UserInterface?.SetState(new PaperBoundUserInterfaceState(paperComp.Content, paperComp.Mode));
        }
    }
}
