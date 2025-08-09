using System.Linq;
using Content.Client.Chemistry.UI.ChemMaster;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Storage;

namespace Content.Client.Chemistry.EntitySystems;

public sealed class ChemMasterSystem : SharedChemMasterSystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChemMasterComponent, AfterAutoHandleStateEvent>(OnAfterState);
    }

    private void OnAfterState(Entity<ChemMasterComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        DirtyUI(ent);
    }

    protected override void DirtyUI(Entity<ChemMasterComponent> ent)
    {
        if (_ui.TryGetOpenUi<ChemMasterBoundUserInterface>(ent.Owner, ChemMasterUiKey.Key, out var ui))
            ui.Update();
    }

    /// <summary>
    /// Creates a <seealso cref="ContainerInfo" /> for a ChemMasterComponent's input buffer.
    /// Used within the ChemMaster BUI.
    /// </summary>
    public ContainerInfo? BuildInputContainerInfo(Entity<ChemMasterComponent> ent)
    {
        if (ItemSlots.GetItemOrNull(ent, ChemMasterComponent.InputSlotName) is not { } container)
            return null;

        if (!TryComp(container, out FitsInDispenserComponent? fits)
            || !SolContainer.TryGetSolution(container, fits.Solution, out _, out var solution))
            return null;

        return new ContainerInfo(Name(container), solution.Volume, solution.MaxVolume)
        {
            Reagents = solution.Contents
        };
    }

    /// <summary>
    /// Creates a <seealso cref="ContainerInfo" /> for a ChemMasterComponent's output buffer.
    /// Used within the ChemMaster BUI.
    /// </summary>
    public ContainerInfo? BuildOutputContainerInfo(Entity<ChemMasterComponent> ent)
    {
        if (ItemSlots.GetItemOrNull(ent, ChemMasterComponent.OutputSlotName) is not { } container)
            return null;

        var name = Name(container);
        if (SolContainer.TryGetSolution(container,
                ChemMasterComponent.BottleSolutionName,
                out _,
                out var solution))
        {
            return new ContainerInfo(name, solution.Volume, solution.MaxVolume) { Reagents = solution.Contents };
        }

        if (!TryComp(container, out StorageComponent? storage))
            return null;

        var pills = storage.Container.ContainedEntities.Select(pill =>
            {
                SolContainer.TryGetSolution(pill,
                    ChemMasterComponent.PillSolutionName,
                    out _,
                    out var pillSol);
                var quantity = pillSol?.Volume ?? FixedPoint2.Zero;
                return (pill, quantity);
            })
            .ToList();

        return new ContainerInfo(name, Storage.GetCumulativeItemAreas((container, storage)), storage.Grid.GetArea())
        {
            Entities = pills.Select(x => (GetNetEntity(x.pill), x.quantity)).ToList(),
        };
    }
}
