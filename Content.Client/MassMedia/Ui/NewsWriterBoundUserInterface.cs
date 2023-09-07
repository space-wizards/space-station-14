using Robust.Shared.Timing;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Content.Shared.MassMedia.Systems;
using Content.Shared.MassMedia.Components;
using Content.Client.GameTicking.Managers;
using Robust.Shared.Utility;

namespace Content.Client.MassMedia.Ui;

[UsedImplicitly]
public sealed class NewsWriterBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private NewsWriterMenu? _menu;

    [Dependency] private readonly IGameTiming _gameTiming = default!;
    private ClientGameTicker? _gameTicker;

    public NewsWriterBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {

    }

    protected override void Open()
    {
        _menu = new NewsWriterMenu(_gameTiming);

        _menu.OpenCentered();
        _menu.OnClose += Close;

        _menu.ArticleEditorPanel.PublishButtonPressed += OnPublishButtonPressed;
        _menu.DeleteButtonPressed += OnDeleteButtonPressed;

        _gameTicker = EntMan.System<ClientGameTicker>();

        SendMessage(new NewsWriterArticlesRequestMessage());
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
        if (state is not NewsWriterBoundUserInterfaceState cast)
            return;

        _menu?.UpdateUI(cast.Articles, cast.PublishEnabled, cast.NextPublish);
    }

    private void OnPublishButtonPressed()
    {
        var title = _menu?.ArticleEditorPanel.TitleField.Text ?? "";
        if (_menu == null || title.Length == 0)
            return;

        var stringContent = Rope.Collapse(_menu.ArticleEditorPanel.ContentField.TextRope);

        if (stringContent.Length == 0 || _gameTicker == null)
            return;

        var name = title.Length <= 100 ? title.Trim() : $"{title.Trim()[..100]}...";
        var article = new NewsArticle
        {
            Name = name,
            Content = stringContent,
            ShareTime = _gameTiming.CurTime.Subtract(_gameTicker.RoundStartTimeSpan)
        };

        SendMessage(new NewsWriterPublishMessage(article));
    }

    private void OnDeleteButtonPressed(int articleNum)
    {
        if (_menu == null) return;

        SendMessage(new NewsWriterDeleteMessage(articleNum));
    }
}
