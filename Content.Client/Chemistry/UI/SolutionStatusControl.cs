using Content.Client.Chemistry.Components;
using Content.Client.Chemistry.EntitySystems;
using Content.Client.Items.UI;
using Content.Client.Message;
using Content.Client.Stylesheets;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Chemistry.UI;

/// <summary>
/// Displays basic solution information for <see cref="SolutionItemStatusComponent"/>.
/// </summary>
/// <seealso cref="SolutionItemStatusSystem"/>
public sealed class SolutionStatusControl : PollingItemStatusControl<SolutionStatusControl.Data>
{
    private readonly Entity<SolutionItemStatusComponent> _parent;
    private readonly IEntityManager _entityManager;
    private readonly SharedSolutionContainerSystem _solutionContainers;
    private readonly RichTextLabel _label;

    public SolutionStatusControl(
        Entity<SolutionItemStatusComponent> parent,
        IEntityManager entityManager,
        SharedSolutionContainerSystem solutionContainers)
    {
        _parent = parent;
        _entityManager = entityManager;
        _solutionContainers = solutionContainers;
        _label = new RichTextLabel { StyleClasses = { StyleClass.ItemStatus } };
        AddChild(_label);
    }

    protected override Data PollData()
    {
        if (!_solutionContainers.TryGetSolution(_parent.Owner, _parent.Comp.Solution, out _, out var solution))
            return default;

        FixedPoint2? transferAmount = null;
        if (_entityManager.TryGetComponent(_parent.Owner, out SolutionTransferComponent? transfer))
            transferAmount = transfer.TransferAmount;

        ExaminedVolumeDisplay? state = null;
        if (_entityManager.TryGetComponent(_parent.Owner, out ExaminableSolutionComponent? examine) &&
            _entityManager.TryGetComponent(_parent.Owner, out TransformComponent? xform))
        {
            state = _solutionContainers.ExaminedVolume((_parent, examine), solution, xform.ParentUid);
        }

        return new Data(solution.Volume, solution.MaxVolume, transferAmount, state);
    }

    protected override void Update(in Data data)
    {
        var markup = "";

        if (data.VolumeState is { } state)
            markup = Loc.GetString(_parent.Comp.LocControlVolume,
                            ("fillLevel", state),
                            ("current", data.CurrentVolume),
                            ("max", data.MaxVolume));

        if (data.TransferVolume is { } transferVolume)
            markup += "\n" + Loc.GetString(_parent.Comp.LocControlTransfer, ("volume", transferVolume));

        _label.SetMarkup(markup);
    }

    public readonly record struct Data(FixedPoint2 CurrentVolume, FixedPoint2 MaxVolume, FixedPoint2? TransferVolume, ExaminedVolumeDisplay? VolumeState);
}
