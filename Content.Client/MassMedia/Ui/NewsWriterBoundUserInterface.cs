using JetBrains.Annotations;
using Content.Shared.MassMedia.Systems;
using Content.Shared.MassMedia.Components;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.MassMedia.Ui;

[UsedImplicitly]
public sealed class NewsWriterBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    [ViewVariables]
    private NewsWriterMenu? _menu;

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
}
