using Content.Client.Message;
using Content.Client.Stylesheets;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client.Chemistry.UI;

public sealed class HyposprayStatusControl : Control
{
    private readonly Entity<HyposprayComponent> _parent;
    private readonly RichTextLabel _label;
    private readonly SharedSolutionContainerSystem _solutionContainers;

    public HyposprayStatusControl(Entity<HyposprayComponent> parent, SharedSolutionContainerSystem solutionContainers)
    {
        _parent = parent;
        _solutionContainers = solutionContainers;
        _label = new RichTextLabel { StyleClasses = { StyleNano.StyleClassItemStatus } };
        AddChild(_label);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (!_solutionContainers.TryGetSolution(_parent.Owner, _parent.Comp.SolutionName, out _, out var solution))
            return;

        var modeStringLocalized = Loc.GetString(_parent.Comp.ToggleMode switch
        {
            HyposprayToggleMode.All => "hypospray-all-mode-text",
            HyposprayToggleMode.OnlyMobs => "hypospray-mobs-only-mode-text",
            _ => "hypospray-invalid-text"
        });

        _label.SetMarkup(Loc.GetString("hypospray-volume-label",
            ("currentVolume", solution.Volume),
            ("totalVolume", solution.MaxVolume),
            ("modeString", modeStringLocalized)));
    }
}
