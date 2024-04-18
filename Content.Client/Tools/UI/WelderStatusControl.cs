using Content.Client.Message;
using Content.Client.Stylesheets;
using Content.Shared.Tools.Components;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client.Tools.UI;

public sealed class WelderStatusControl : Control
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    private readonly ToolSystem _tool;

    private readonly Entity<WelderComponent> _parent;
    private readonly RichTextLabel _label;

    public WelderStatusControl(Entity<WelderComponent> parent)
    {
        IoCManager.InjectDependencies(this);

        _parent = parent;
        var entMan = IoCManager.Resolve<IEntityManager>();
        _tool = entMan.System<ToolSystem>();

        _label = new RichTextLabel { StyleClasses = { StyleNano.StyleClassItemStatus } };
        AddChild(_label);

        UpdateDraw();
    }

    /// <inheritdoc />
    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        Update();
    }

    public void Update()
    {
        if (!_gameTiming.IsFirstTimePredicted)
            return;

        var (fuel, fuelCap) = _tool.GetWelderFuelAndCapacity(_parent, _parent);
        var lit = _parent.Comp.Enabled;

        _label.SetMarkup(Loc.GetString("welder-component-on-examine-detailed-message",
            ("colorName", fuel < fuelCap / 4f ? "darkorange" : "orange"),
            ("fuelLeft", Math.Round(fuel.Float(), 1)),
            ("fuelCapacity", fuelCap),
            ("status", Loc.GetString(lit ? "welder-component-on-examine-welder-lit-message" : "welder-component-on-examine-welder-not-lit-message"))));
    }
}
