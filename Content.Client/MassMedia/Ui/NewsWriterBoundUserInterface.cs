using JetBrains.Annotations;
using Content.Shared.MassMedia.Systems;
using Content.Shared.MassMedia.Components;
using Robust.Client.UserInterface;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.MassMedia.Ui;

[UsedImplicitly]
public sealed partial class NewsWriterBoundUserInterface : BoundUserInterface
{
    private static readonly EntityTimerId PublishTimer = new("publish");
    private static readonly EntityTimerId RefreshTimer = new("refresh");

    [Dependency] private IGameTiming _timing = default!;

    [ViewVariables]
    private NewsWriterMenu? _menu;

    public NewsWriterBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {

    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<NewsWriterMenu>();

        _menu.ArticleEditorPanel.PublishButtonPressed += OnPublishButtonPressed;
        _menu.DeleteButtonPressed += OnDeleteButtonPressed;

        _menu.CreateButtonPressed += OnCreateButtonPressed;
        _menu.ArticleEditorPanel.ArticleDraftUpdated += OnArticleDraftUpdated;

        SendMessage(new NewsWriterArticlesRequestMessage());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is not NewsWriterBoundUserInterfaceState cast)
            return;

        _menu?.UpdateUI(cast.Articles, cast.PublishEnabled, cast.NextPublish, cast.DraftTitle, cast.DraftContent);
        _menu?.UpdatePublishTime(_timing.CurTime);

        if (cast.NextPublish <= _timing.CurTime)
        {
            CancelTimer(PublishTimer);
            CancelTimer(RefreshTimer);
            return;
        }

        SetTimerAt(PublishTimer, cast.NextPublish);
        SetTimer(RefreshTimer, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
    }

    protected override void OnTimer(EntityTimerEvent timer)
    {
        if (timer.Id == PublishTimer)
            CancelTimer(RefreshTimer);

        if (timer.Id == PublishTimer || timer.Id == RefreshTimer)
            _menu?.UpdatePublishTime(timer.FiredAt);
    }

    private void OnPublishButtonPressed()
    {
        var title = _menu?.ArticleEditorPanel.TitleField.Text.Trim() ?? "";
        if (_menu == null || title.Length == 0)
            return;

        var stringContent = Rope.Collapse(_menu.ArticleEditorPanel.ContentField.TextRope).Trim();

        if (stringContent.Length == 0)
            return;

        var name = title.Length <= SharedNewsSystem.MaxTitleLength
            ? title
            : $"{title[..(SharedNewsSystem.MaxTitleLength - 3)]}...";

        var content = stringContent.Length <= SharedNewsSystem.MaxContentLength
            ? stringContent
            : $"{stringContent[..(SharedNewsSystem.MaxContentLength - 3)]}...";


        SendMessage(new NewsWriterPublishMessage(name, content));
    }

    private void OnDeleteButtonPressed(int articleNum)
    {
        if (_menu == null)
            return;

        SendMessage(new NewsWriterDeleteMessage(articleNum));
    }

    private void OnCreateButtonPressed()
    {
        SendMessage(new NewsWriterRequestDraftMessage());
    }

    private void OnArticleDraftUpdated(string title, string content)
    {
        SendMessage(new NewsWriterSaveDraftMessage(title, content));
    }
}
