using Robust.Client.UserInterface;

namespace Content.Client.Xenoarchaeology.Ui;

/// <summary>
/// BUI for hand-held xeno artifact scanner,  server-provided UI updates.
/// </summary>
public sealed class ArtifactNukerBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private ArtifactNukerMenu? _menu;

    /// <inheritdoc />
    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<ArtifactNukerMenu>();

        _menu.IndexChanged += name =>
        {
            SendMessage(new BorgSetNameBuiMessage(name));
        };
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;

        _menu?.Dispose();
    }
}
