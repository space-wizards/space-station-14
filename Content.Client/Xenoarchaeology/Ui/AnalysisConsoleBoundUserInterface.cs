using Content.Shared.Research.Components;
using Content.Shared.Xenoarchaeology.Equipment.Components;
using JetBrains.Annotations;

namespace Content.Client.Xenoarchaeology.Ui;

[UsedImplicitly]
public sealed class AnalysisConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private AnalysisConsoleMenu? _consoleMenu;

    /// <inheritdoc />
    protected override void Open()
    {
        base.Open();

        _consoleMenu = new AnalysisConsoleMenu(Owner);

        _consoleMenu.OnClose += Close;
        _consoleMenu.OpenCentered();

        _consoleMenu.OnServerSelectionButtonPressed += () =>
        {
            SendMessage(new ConsoleServerSelectionMessage());
        };
        _consoleMenu.OnExtractButtonPressed += () =>
        {
            SendMessage(new AnalysisConsoleExtractButtonPressedMessage());
        };
    }

    /// <summary>
    /// Update UI state based on corresponding component.
    /// </summary>
    public void Update(Entity<AnalysisConsoleComponent> ent)
    {
        _consoleMenu?.Update(ent);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;

        _consoleMenu?.Dispose();
    }
}

