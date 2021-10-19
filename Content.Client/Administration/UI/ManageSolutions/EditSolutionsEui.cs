using Content.Client.Eui;
using Content.Shared.Administration;
using Content.Shared.Eui;
using JetBrains.Annotations;

namespace Content.Client.Administration.UI.ManageSolutions
{
    [UsedImplicitly]
    public sealed class EditSolutionsEui : BaseEui
    {
        private readonly EditSolutionsWindow _window;

        public EditSolutionsEui()
        {
            _window = new EditSolutionsWindow();
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

        public override void HandleState(EuiStateBase baseState)
        {
            var state = (EditSolutionsEuiState) baseState;
            _window.SetTarget(state.Target);
            _window.UpdateSolutions(state.Solutions);
        }
    }
}
