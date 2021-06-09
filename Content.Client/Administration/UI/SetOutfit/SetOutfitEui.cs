using Content.Client.Eui;
using Content.Shared.Eui;
using JetBrains.Annotations;
using Content.Shared.Administration;

namespace Content.Client.UserInterface.AdminMenu.SetOutfit
{
    [UsedImplicitly]
    public sealed class SetOutfitEui : BaseEui
    {
        private readonly SetOutfitMenu _window;
        public SetOutfitEui()
        {
            _window = new SetOutfitMenu();
        }

        public override void Opened()
        {
            _window.OpenCentered();
        }

        public override void Closed()
        {
            base.Closed();
            _window.Close();
        }

        public override void HandleState(EuiStateBase state)
        {
            var outfitState = (SetOutfitEuiState) state;
            _window.TargetEntityId = outfitState.TargetEntityId;

        }
    }
}
