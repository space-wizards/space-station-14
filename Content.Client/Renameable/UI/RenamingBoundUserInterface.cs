using Content.Shared.Renameable;
using Robust.Client.GameObjects;

namespace Content.Client.Renameable.UI;

/// <summary>
/// Initializes a <see cref="RenamingWindow"/>.
/// </summary>
public sealed class RenamingBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IEntityManager _entMan = default!;

    [ViewVariables]
    private RenamingWindow? _window;

    public RenamingBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        var name = _entMan.GetComponent<MetaDataComponent>(Owner).EntityName;
        _window = new RenamingWindow(name);

        _window.OpenCentered();

        _window.OnClose += Close;
        _window.OnRenamed += newName => SendMessage(new RenamedMessage(newName));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
            _window?.Dispose();
    }
}
