using Content.Shared.Construction.Components;
using JetBrains.Annotations;

namespace Content.Client.Construction.UI
{
    [UsedImplicitly]
    public sealed class FlatpackCreatorBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private FlatpackCreatorMenu? _menu;

        public FlatpackCreatorBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _menu = new FlatpackCreatorMenu(Owner);
            _menu.OnClose += Close;

            _menu.PackButtonPressed += () =>
            {
                SendMessage(new FlatpackCreatorStartPackBuiMessage());
            };

            _menu.OpenCentered();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;

            _menu?.Dispose();
        }
    }
}
