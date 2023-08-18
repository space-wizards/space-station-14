using Content.Server.Medical.Components;
using Content.Shared.CartridgeLoader;
using FastAccessors.Monads;

namespace Content.Server.CartridgeLoader.Cartridges;

/// <summary>
/// Handles install and removal of Health Analyzer Cartridge into a PDA
/// </summary>
public sealed class HealthAnalyzerCartridgeSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HealthAnalyzerCartridgeComponent, CartridgeAddedEvent>(OnAdded);
        SubscribeLocalEvent<HealthAnalyzerCartridgeComponent, CartridgeRemovedEvent>(OnRemoved);
    }

    /// <summary>
    /// Attaches the <see cref="HealthAnalyzerComponent" /> to the PDA when the program is installed.
    /// </summary>
    private void OnAdded(EntityUid uid, HealthAnalyzerCartridgeComponent component, CartridgeAddedEvent args)
    {
        var healthAnalyzerComponent = EnsureComp<HealthAnalyzerComponent>(args.Loader);
        healthAnalyzerComponent.ScanDelay = component.ScanDelay;
        healthAnalyzerComponent.ScanningBeginSound = component.ScanningBeginSound;
        healthAnalyzerComponent.ScanningEndSound = component.ScanningEndSound;
    }

    /// <summary>
    /// Removes the <see cref="HealthAnalyzerComponent" /> from the PDA entity when the program is removed.
    /// </summary>
    private void OnRemoved(EntityUid uid, HealthAnalyzerCartridgeComponent component, CartridgeRemovedEvent args)
    {
        RemComp<HealthAnalyzerComponent>(args.Loader);
    }
}
