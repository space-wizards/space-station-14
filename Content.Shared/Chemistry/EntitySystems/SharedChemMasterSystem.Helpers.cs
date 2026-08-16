using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;

namespace Content.Shared.Chemistry.EntitySystems;

public abstract partial class SharedChemMasterSystem
{
    private void TransferReagents(Entity<ChemMasterComponent> chemMaster, ReagentId id, FixedPoint2 amount, bool fromBuffer)
    {
        var container = _itemSlotsSystem.GetItemOrNull(chemMaster.Owner, SharedChemMaster.InputSlotName);
        if (container is null ||
            !_solutionContainerSystem.TryGetFitsInDispenser(container.Value, out var containerSoln, out var containerSolution) ||
            !_solutionContainerSystem.TryGetSolution(chemMaster.Owner, SharedChemMaster.BufferSolutionName, out _, out var bufferSolution))
        {
            return;
        }

        if (fromBuffer) // Buffer to container
        {
            amount = FixedPoint2.Min(amount, containerSolution.AvailableVolume);
            amount = bufferSolution.RemoveReagent(id, amount, preserveOrder: true);
            _solutionContainerSystem.TryAddReagent(containerSoln.Value, id, amount, out var _);
        }
        else // Container to buffer
        {
            amount = FixedPoint2.Min(amount, containerSolution.GetReagentQuantity(id));
            _solutionContainerSystem.RemoveReagent(containerSoln.Value, id, amount);
            bufferSolution.AddReagent(id, amount);
        }

        Dirty(chemMaster);
        UpdateUiLabels(chemMaster);
    }

    private void DiscardReagents(Entity<ChemMasterComponent> chemMaster, ReagentId id, FixedPoint2 amount, bool fromBuffer)
    {
        if (fromBuffer)
        {
            if (_solutionContainerSystem.TryGetSolution(chemMaster.Owner, SharedChemMaster.BufferSolutionName, out _, out var bufferSolution))
                bufferSolution.RemoveReagent(id, amount, preserveOrder: true);
            else
                return;
        }
        else
        {
            var container = _itemSlotsSystem.GetItemOrNull(chemMaster.Owner, SharedChemMaster.InputSlotName);
            if (container is not null &&
                _solutionContainerSystem.TryGetFitsInDispenser(container.Value, out var containerSolution, out _))
            {
                _solutionContainerSystem.RemoveReagent(containerSolution.Value, id, amount);
            }
            else
                return;
        }

        Dirty(chemMaster);
        UpdateUiLabels(chemMaster);
    }

    private void ClickSound(Entity<ChemMasterComponent> chemMaster, EntityUid? user = null)
    {
        var audioParams = chemMaster.Comp.ClickSound?.Params ?? AudioParams.Default;
        audioParams = audioParams.AddVolume(-2f);
        _audioSystem.PlayPredicted(chemMaster.Comp.ClickSound, chemMaster.Owner, user, audioParams);
    }
}
