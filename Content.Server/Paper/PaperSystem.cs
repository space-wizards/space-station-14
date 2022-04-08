using Content.Server.UserInterface;
using Content.Shared.Examine;
using Content.Shared.Paper;

namespace Content.Server.Paper
{
    public sealed class PaperSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PaperComponent, BeforeActivatableUIOpenEvent>(AfterUIOpen);
            SubscribeLocalEvent<PaperComponent, ExaminedEvent>(OnExamined);
        }

        private void AfterUIOpen(EntityUid uid, PaperComponent component, BeforeActivatableUIOpenEvent args)
        {
            component.Mode = SharedPaperComponent.PaperAction.Read;
            component.UpdateUserInterface();
        }

        private void OnExamined(EntityUid uid, PaperComponent component, ExaminedEvent args)
        {
            if (!args.IsInDetailsRange)
                return;
            if (component.Content == "")
                return;

            args.PushMarkup(
                Loc.GetString(
                    "paper-component-examine-detail-has-words"
                )
            );
        }
    }
}
