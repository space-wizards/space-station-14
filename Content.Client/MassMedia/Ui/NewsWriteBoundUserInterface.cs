using Robust.Shared.Timing;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Content.Shared.MassMedia.Systems;
using Content.Shared.MassMedia.Components;
using Content.Client.GameTicking.Managers;
using Robust.Shared.Utility;

namespace Content.Client.MassMedia.Ui
{
    [UsedImplicitly]
    public sealed class NewsWriteBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private NewsWriteMenu? _menu;

        [Dependency] private readonly IEntitySystemManager _entitySystem = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        private ClientGameTicker? _gameTicker;

        [ViewVariables]
        private string _windowName = Loc.GetString("news-read-ui-default-title");

        public NewsWriteBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {

        }

        protected override void Open()
        {
            _menu = new NewsWriteMenu(_windowName);

            _menu.OpenCentered();
            _menu.OnClose += Close;

            _menu.ShareButtonPressed += OnShareButtonPressed;
            _menu.DeleteButtonPressed += num => OnDeleteButtonPressed(num);

            _gameTicker = _entitySystem.GetEntitySystem<ClientGameTicker>();

            SendMessage(new NewsWriteArticlesRequestMessage());
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
            if (_menu == null || state is not NewsWriteBoundUserInterfaceState cast)
                return;

            _menu.UpdateUI(cast.Articles);
        }

        private void OnShareButtonPressed()
        {
            if (_menu == null || _menu.NameInput.Text.Length == 0)
                return;

            var stringContent = Rope.Collapse(_menu.ContentInput.TextRope);

            if (stringContent == null || stringContent.Length == 0) return;
            if (_gameTicker == null) return;

            NewsArticle article = new NewsArticle();
            article.Name = _menu.NameInput.Text;
            article.Content = stringContent;
            article.ShareTime = _gameTiming.CurTime.Subtract(_gameTicker.RoundStartTimeSpan);

            SendMessage(new NewsWriteShareMessage(article));
        }

        private void OnDeleteButtonPressed(int articleNum)
        {
            if (_menu == null) return;

            SendMessage(new NewsWriteDeleteMessage(articleNum));
        }
    }
}
