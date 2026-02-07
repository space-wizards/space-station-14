using Content.Client.Message;
using Content.Client.Stylesheets;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.Chemistry.UI;

public sealed class InjectorStatusControl : Control
{
    private readonly IPrototypeManager _prototypeManager;

    private readonly Entity<InjectorComponent> _parent;
    private readonly SharedSolutionContainerSystem _solutionContainers;
    private readonly RichTextLabel _label;

    private FixedPoint2 _prevVolume;
    private FixedPoint2 _prevMaxVolume;
    private FixedPoint2? _prevTransferAmount;
    private InjectorBehavior _prevBehavior;

    public InjectorStatusControl(Entity<InjectorComponent> parent, SharedSolutionContainerSystem solutionContainers, IPrototypeManager prototypeManager)
    {
        _prototypeManager  = prototypeManager;

        _parent = parent;
        _solutionContainers = solutionContainers;
        _label = new RichTextLabel { StyleClasses = { StyleClass.ItemStatus } };
        AddChild(_label);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (!_solutionContainers.TryGetSolution(_parent.Owner, _parent.Comp.SolutionName, out _, out var solution)
            || !_prototypeManager.Resolve(_parent.Comp.ActiveModeProtoId, out var activeMode))
            return;

        // only updates the UI if any of the details are different than they previously were
        if (_prevVolume == solution.Volume
            && _prevMaxVolume == solution.MaxVolume
            && _prevTransferAmount == _parent.Comp.CurrentTransferAmount
            && _prevBehavior == activeMode.Behavior)
            return;

        _prevVolume = solution.Volume;
        _prevMaxVolume = solution.MaxVolume;
        _prevTransferAmount = _parent.Comp.CurrentTransferAmount;
        _prevBehavior = activeMode.Behavior;

        // Update current volume and injector state
        // Seeing transfer volume is only important for injectors that can change it.
        if (activeMode.TransferAmounts.Count > 1 && _parent.Comp.CurrentTransferAmount.HasValue)
        {
            _label.SetMarkup(Loc.GetString("injector-volume-transfer-label",
                ("currentVolume", solution.Volume),
                ("totalVolume", solution.MaxVolume),
                ("modeString", Loc.GetString(activeMode.Name)),
                ("transferVolume", _parent.Comp.CurrentTransferAmount.Value)));
        }
        else
        {
            _label.SetMarkup(Loc.GetString("injector-volume-label",
                ("currentVolume", solution.Volume),
                ("totalVolume", solution.MaxVolume),
                ("modeString", Loc.GetString(activeMode.Name))));
        }
    }
}
