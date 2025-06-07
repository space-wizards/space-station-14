using Content.Client.SprayPainter.Airlocks;
using Content.Client.SprayPainter.Airlocks.UI;
using Content.Client.SprayPainter.AtmosPipes.UI;
using Content.Shared.SprayPainter.Airlocks;
using Content.Shared.SprayPainter.Airlocks.Components;
using Content.Shared.SprayPainter.AtmosPipes;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.SprayPainter.UI;

[UsedImplicitly]
public sealed class SprayPainterBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    protected override void Open()
    {
        base.Open();

        var window = this.CreateWindow<SprayPainterWindow>();

        if (EntMan.TryGetComponent(Owner, out AirlockPainterComponent? airlockPainter))
        {
            window.AddContent(
                new AirlockPainterWindow(
                    EntMan.System<AirlockPainterSystem>().Entries,
                    airlockPainter,
                    styleIndex => SendMessage(new AirlockPainterSpritePickedMessage(styleIndex))
                )
            );
        }

        if (EntMan.TryGetComponent(Owner, out AtmosPipePainterComponent? pipePainter))
        {
            window.AddContent(
                new AtmosPipePainterWindow(
                    pipePainter,
                    colorKey => SendMessage(new AtmosPipePainterColorPickedMessage(colorKey))
                )
            );
        }
    }
}
