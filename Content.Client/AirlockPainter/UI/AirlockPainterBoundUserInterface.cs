using Content.Shared.AirlockPainter;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.AirlockPainter.UI
{
    public sealed class AirlockPainterBoundUserInterface : BoundUserInterface
    {
        private AirlockPainterWindow? _window;
        private AirlockPainterSystem? _painter;

        [Dependency] private readonly IEntitySystemManager _entitySystems = default!;

        public AirlockPainterBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _window = new AirlockPainterWindow();

            _painter = _entitySystems.GetEntitySystem<AirlockPainterSystem>();

            _window.OpenCentered();
            _window.OnClose += Close;
            _window.OnSpritePicked = OnSpritePicked;
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (_window == null)
                return;

            if (_painter == null)
                return;

            if (state is not AirlockPainterBoundUserInterfaceState stateCast)
                return;

            _window.Populate(_painter.Entries, stateCast.SelectedStyle);
        }

        private void OnSpritePicked(ItemList.ItemListSelectedEventArgs args)
        {
            SendMessage(new AirlockPainterSpritePickedMessage(args.ItemIndex));
        }
    }
}
