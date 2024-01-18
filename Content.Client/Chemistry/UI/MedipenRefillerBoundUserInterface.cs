using Content.Shared.Chemistry;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Chemistry.UI
{
    [UsedImplicitly]
    public sealed class MedipenRefillerBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private MedipenRefillerWindow? _window;

        public MedipenRefillerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _window = new MedipenRefillerWindow
            {
                Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName,
            };

            _window.OpenCentered();
            _window.OnClose += Close;
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            switch (state)
            {
                case MedipenRefillerUpdateState msg:
                    if (_window != null)
                        _window.Recipes = msg.Recipes;
                    _window?.UpdateRecipes();
                    break;
            }
        }
    }
}
