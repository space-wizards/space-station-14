using Content.Shared.Construction.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

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

            _menu = this.CreateWindow<FlatpackCreatorMenu>();
            _menu.SetEntity(Owner);

            _menu.PackButtonPressed += () =>
            {
                SendMessage(new FlatpackCreatorStartPackBuiMessage());
            };

            _menu.OpenCentered();
        }
    }
}
