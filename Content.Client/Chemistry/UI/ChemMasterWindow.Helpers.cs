using System.Linq;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Storage;

namespace Content.Client.Chemistry.UI;

public sealed partial class ChemMasterWindow
{
    private ContainerInfo? BuildInputContainerInfo(EntityUid? container)
    {
        if (container is not { Valid: true })
            return null;

        if (!_entityManager.TryGetComponent(container, out FitsInDispenserComponent? fits)
            || !_solutionContainer.TryGetSolution(container.Value, fits.Solution, out _, out var solution))
        {
            return null;
        }

        return BuildContainerInfo(EntityName(container.Value), solution);
    }

    private ContainerInfo? BuildOutputContainerInfo(EntityUid? container)
    {
        if (container is not { Valid: true })
        {
            Log.Info("No output container found");
            return null;
        }

        var name = EntityName(container.Value);
        if (_solutionContainer.TryGetSolution(
                container.Value,
                SharedChemMaster.BottleSolutionName,
                out _,
                out var solution))
        {
            Log.Info("Output container found");
            return BuildContainerInfo(name, solution);
        }


        if (!_entityManager.TryGetComponent(container, out StorageComponent? storage))
        {
            Log.Info("no storage");
            return null;
        }

        var pills = storage.Container.ContainedEntities
            .Select((Func<EntityUid, (string, FixedPoint2 quantity)>) (pill =>
        {
            _solutionContainer.TryGetSolution(pill, SharedChemMaster.PillSolutionName, out _, out solution);
            var quantity = solution?.Volume ?? FixedPoint2.Zero;
            return (EntityName(pill), quantity);
        }))
        .ToList();

        return new ContainerInfo(name, _storage.GetCumulativeItemAreas((container.Value, storage)), storage.Grid.GetArea())
        {
            Entities = pills
        };
    }

    private static ContainerInfo BuildContainerInfo(string name, Solution solution)
    {
        return new ContainerInfo(name, solution.Volume, solution.MaxVolume)
        {
            Reagents = solution.Contents
        };
    }

    private string EntityName(EntityUid uid, MetaDataComponent? metaData = null)
    {
        return !_metaQuery.Resolve(uid, ref metaData, false) ? string.Empty : metaData.EntityName;
    }

}
