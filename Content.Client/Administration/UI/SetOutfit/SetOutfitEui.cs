using Content.Client.Eui;
using Content.Shared.Administration;
using Content.Shared.Eui;
using JetBrains.Annotations;

namespace Content.Client.Administration.UI.SetOutfit
{
    [UsedImplicitly]
    public sealed class SetOutfitEui : BaseEui
    {
        private readonly SetOutfitMenu _window;
        private IEntityManager _entManager;

        public SetOutfitEui()
        {
            _entManager = IoCManager.Resolve<IEntityManager>();
            _window = new SetOutfitMenu();
            _window.OnClose += OnClosed;
        }

        private void OnClosed()
        {
            SendMessage(new CloseEuiMessage());
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
            _window.TargetEntityId = outfitState.TargetNetEntity;

        }
    }
}
