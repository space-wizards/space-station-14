using Content.Server.UserInterface;
using Content.Shared.Paper;

namespace Content.Server.Paper
{
    public sealed class PaperSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PaperComponent, BeforeActivatableUIOpenEvent>(AfterUIOpen);
        }

        private void AfterUIOpen(EntityUid uid, PaperComponent component, BeforeActivatableUIOpenEvent args)
        {
            component.Mode = SharedPaperComponent.PaperAction.Read;
            component.UpdateUserInterface();
        }
    }
}
