using Content.Client.Message;
using Content.Client.Stylesheets;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client.Chemistry.UI;

public sealed class InjectorStatusControl : Control
{
    private readonly Entity<InjectorComponent> _parent;
    private readonly SharedSolutionContainerSystem _solutionContainers;
    private readonly RichTextLabel _label;

    public InjectorStatusControl(Entity<InjectorComponent> parent, SharedSolutionContainerSystem solutionContainers)
    {
        _parent = parent;
        _solutionContainers = solutionContainers;
        _label = new RichTextLabel { StyleClasses = { StyleNano.StyleClassItemStatus } };
        AddChild(_label);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (!_solutionContainers.TryGetSolution(_parent.Owner, InjectorComponent.SolutionName, out _, out var solution))
            return;

        // Update current volume and injector state
        var modeStringLocalized = Loc.GetString(_parent.Comp.ToggleState switch
        {
            InjectorToggleMode.Draw => "injector-draw-text",
            InjectorToggleMode.Inject => "injector-inject-text",
            _ => "injector-invalid-injector-toggle-mode"
        });

        _label.SetMarkup(Loc.GetString("injector-volume-label",
            ("currentVolume", solution.Volume),
            ("totalVolume", solution.MaxVolume),
            ("modeString", modeStringLocalized),
            ("transferVolume", _parent.Comp.TransferAmount)));
    }
}
