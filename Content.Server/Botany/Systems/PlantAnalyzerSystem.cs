using System.Diagnostics.CodeAnalysis;
using Content.Server.AbstractAnalyzer;
using Content.Server.Botany.Components;
using Content.Shared.Botany.PlantAnalyzer;
using Content.Shared.Interaction;
using Robust.Server.GameObjects;

namespace Content.Server.Botany.Systems;

public sealed class PlantAnalyzerSystem : AbstractAnalyzerSystem<PlantAnalyzerComponent, PlantAnalyzerDoAfterEvent>
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    /// <inheritdoc/>
    public override void UpdateScannedUser(EntityUid analyzer, EntityUid target, bool scanMode)
    {
        if (!_uiSystem.HasUi(analyzer, PlantAnalyzerUiKey.Key))
            return;

        if (!ValidScanTarget(target))
            return;

        PlantAnalyzerSeedData? seedData = null;
        if (_entityManager.TryGetComponent<PlantHolderComponent>(target, out var plantHolder) && plantHolder.Seed is not null)
            seedData = new PlantAnalyzerSeedData(plantHolder.Seed.DisplayName);

        // TODO: PA
        _uiSystem.ServerSendUiMessage(analyzer, PlantAnalyzerUiKey.Key, new PlantAnalyzerScannedUserMessage(
            GetNetEntity(target),
            scanMode,
            seedData
        ));
    }

    /// <inheritdoc/>
    protected override Enum GetUiKey()
    {
        return PlantAnalyzerUiKey.Key;
    }

    /// <inheritdoc/>
    protected override bool ScanTargetPopupMessage(Entity<PlantAnalyzerComponent> uid, AfterInteractEvent args, [NotNullWhen(true)] out string? message)
    {
        message = null;
        return false;
    }

    /// <inheritdoc/>
    protected override bool ValidScanTarget(EntityUid? target)
    {
        return HasComp<PlantHolderComponent>(target);
    }
}
