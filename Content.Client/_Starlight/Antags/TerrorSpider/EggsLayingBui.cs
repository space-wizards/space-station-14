using Content.Client._Starlight.Antags.Abductor;
using Content.Client.RCD;
using Content.Shared._Starlight.Antags.TerrorSpider;
using Content.Shared.Starlight.Antags.Abductor;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Content.Shared.RCD;
using Content.Shared.RCD.Components;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;
using JetBrains.Annotations;

namespace Content.Client._Starlight.Antags.TerrorSpider;

[UsedImplicitly]
public sealed class EggsLayingBui : BoundUserInterface
{
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IClyde _displayManager = default!;

    private EggsLayingMenu? _menu;
    public EggsLayingBui(EntityUid owner, Enum uiKey) : base(owner, uiKey) => IoCManager.InjectDependencies(this);
    protected override void Open()
    {
        _menu = this.CreateWindow<EggsLayingMenu>();
        _menu.OnClose += Close;
        _menu.EggChosen += (egg) =>
        {
            SendMessage(new EggsLayingBuiMsg() { Egg = egg });
            _menu.Close();
            Close();
        };
        var vpSize = _displayManager.ScreenSize;
        _menu.OpenCenteredAt(_inputManager.MouseScreenPosition.Position / vpSize);
    }
    protected override void UpdateState(BoundUserInterfaceState? state)
    {
    }
}
