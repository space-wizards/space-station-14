using Robust.Client.UserInterface;
using Content.Shared.Xenoarchaeology.Equipment;

namespace Content.Client.Xenoarchaeology.Ui;

public sealed class ArtifactNukerBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private ArtifactNukerMenu? _menu;

    /// <inheritdoc />
    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<ArtifactNukerMenu>();
        _menu.GetIndex(Owner);

        _menu.IndexChanged += index =>
        {
            SendMessage(new ArtifactNukerIndexChangeMessage(index));
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
