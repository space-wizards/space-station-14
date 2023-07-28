using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Content.Shared.MassMedia.Components;
using Robust.Shared.Timing;

namespace Content.Client.MassMedia.Ui
{
    [UsedImplicitly]
    public sealed class NewsReadBoundUserInterface : BoundUserInterface
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        [ViewVariables]
        private NewsReadMenu? _menu;

        [ViewVariables]
        private string _windowName = Loc.GetString("news-read-ui-default-title");

        public NewsReadBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            _menu = new NewsReadMenu(_windowName);

            _menu.OpenCentered();
            _menu.OnClose += Close;

            _menu.NextButtonPressed += () => SendMessage(new NewsReadNextMessage());
            _menu.PastButtonPressed += () => SendMessage(new NewsReadPrevMessage());

            SendMessage(new NewsReadArticleRequestMessage());
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;

            _menu?.Close();
            _menu?.Dispose();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            if (_menu == null)
                return;

            if (state is NewsReadBoundUserInterfaceState cast)
                _menu.UpdateUI(cast.Article, cast.TargetNum, cast.TotalNum);
            if (state is NewsReadEmptyBoundUserInterfaceState)
                _menu.UpdateEmptyUI();
        }
    }
}
