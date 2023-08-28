using Content.Shared.Lathe;
using Content.Shared.Research.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Lathe.UI
{
    [UsedImplicitly]
    public sealed class LatheBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private LatheMenu? _menu;

        [ViewVariables]
        private LatheMaterialsEjectionMenu? _materialsEjectionMenu;

        public LatheBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _menu = new LatheMenu(this);
            _materialsEjectionMenu = new LatheMaterialsEjectionMenu();

            _menu.OnClose += Close;

            _menu.OnMaterialsEjectionButtonPressed += _ =>
            {
                if (_materialsEjectionMenu.IsOpen)
                    _materialsEjectionMenu.Close();
                else
                    _materialsEjectionMenu.OpenCenteredRight();
            };

            _menu.OnServerListButtonPressed += _ =>
            {
                SendMessage(new ConsoleServerSelectionMessage());
            };

            _menu.RecipeQueueAction += (recipe, amount) =>
            {
                SendMessage(new LatheQueueRecipeMessage(recipe, amount));
            };

            _materialsEjectionMenu.OnEjectPressed += (material, sheetsToExtract) =>
            {
                SendMessage(new LatheEjectMaterialMessage(material, sheetsToExtract));
            };

            _menu.OpenCentered();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            switch (state)
            {
                case LatheUpdateState msg:
                    if (_menu != null)
                        _menu.Recipes = msg.Recipes;
                    _menu?.PopulateRecipes(Owner);
                    _menu?.PopulateMaterials(Owner);
                    _menu?.PopulateQueueList(msg.Queue);
                    _menu?.SetQueueInfo(msg.CurrentlyProducing);
                    _materialsEjectionMenu?.PopulateMaterials(Owner);
                    break;
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;
            _menu?.Dispose();
            _queueMenu?.Dispose();
            _materialsEjectionMenu?.Dispose();
        }
    }
}
