using Content.Client.Eui;
using Content.Shared.Administration;
using Content.Shared.Eui;
using JetBrains.Annotations;

namespace Content.Client.Administration.UI.ManageSolutions
{
    /// <summary>
    ///     Admin Eui for displaying and editing the reagents in a solution.
    /// </summary>
    [UsedImplicitly]
    public sealed class EditSolutionsEui : BaseEui
    {
        private readonly EditSolutionsWindow _window;

        public EditSolutionsEui()
        {
            _window = new EditSolutionsWindow();
            _window.OnClose += () => SendMessage(new CloseEuiMessage());
        }

        public override void Opened()
        {
            base.Opened();
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
            _window.SetTargetEntity(state.Target);
            _window.UpdateSolutions(state.Solutions);
            _window.UpdateReagents();
        }
    }
}
