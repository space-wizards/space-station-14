using Content.Shared.Chat;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.Input;

namespace Content.Client.Chat.UI;

[UsedImplicitly]
public sealed class EmotesMenuBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IClyde _displayManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;

    private EmotesMenu? _menu;

    public EmotesMenuBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        _menu = new(Owner, this);
        _menu.OnClose += Close;
        _menu.OnPlayEmote += protoId => { SendMessage(new PlayEmoteMessage(protoId)); };

        // Open the menu, centered on the mouse
        var vpSize = _displayManager.ScreenSize;
        _menu.OpenCenteredAt(_inputManager.MouseScreenPosition.Position / vpSize);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;

        _menu?.Dispose();
    }
}
