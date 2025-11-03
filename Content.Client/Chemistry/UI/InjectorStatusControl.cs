using Content.Client.Message;
using Content.Client.Stylesheets;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client.Chemistry.UI;

public sealed class InjectorStatusControl : Control
{
    private readonly Entity<InjectorComponent> _parent;
    private readonly SharedSolutionContainerSystem _solutionContainers;
    private readonly RichTextLabel _label;

    private FixedPoint2 PrevVolume;
    private FixedPoint2 PrevMaxVolume;
    private FixedPoint2 PrevTransferAmount;
    private InjectorToggleMode PrevToggleState;

    public InjectorStatusControl(Entity<InjectorComponent> parent, SharedSolutionContainerSystem solutionContainers)
    {
        _parent = parent;
        _solutionContainers = solutionContainers;
        _label = new RichTextLabel { StyleClasses = { StyleClass.ItemStatus } };
        AddChild(_label);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (!_solutionContainers.TryGetSolution(_parent.Owner, _parent.Comp.SolutionName, out _, out var solution))
            return;

        // only updates the UI if any of the details are different than they previously were
        if (PrevVolume == solution.Volume
            && PrevMaxVolume == solution.MaxVolume
            && PrevTransferAmount == _parent.Comp.CurrentTransferAmount
            && PrevToggleState == _parent.Comp.ToggleState)
            return;

        PrevVolume = solution.Volume;
        PrevMaxVolume = solution.MaxVolume;
        PrevTransferAmount = _parent.Comp.CurrentTransferAmount;
        PrevToggleState = _parent.Comp.ToggleState;

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
            ("transferVolume", _parent.Comp.CurrentTransferAmount)));
    }
}
