using System.Linq;
using Content.Shared.Forensics.Components;
using Content.Shared.Forensics.Systems;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Server.Forensics.Systems;

public sealed partial class ForensicScannerSystem : SharedForensicScannerSystem
{
    [Dependency] private ForensicsSystem _forensicsSystem = default!;
    [Dependency] private TagSystem _tag = default!;

    private static readonly ProtoId<TagPrototype> DnaSolutionScannableTag = "DNASolutionScannable";

    /// <summary>
    /// This override will give the client the data it needs to properly predict the UI when it is finished.
    /// </summary>
    /// <param name="scanner">The scanning forensic scanner.</param>
    /// <param name="user">The entity using the scanner.</param>
    /// <param name="target">The entity being scanned.</param>
    protected override void StartScan(Entity<ForensicScannerComponent> scanner, EntityUid user, EntityUid target)
    {
        base.StartScan(scanner, user, target);

        var component = scanner.Comp;

        if (TryComp<ForensicsComponent>(target, out var forensics))
        {
            component.Fingerprints = forensics.Fingerprints.ToList();
            component.Fibers = forensics.Fibers.ToList();
            component.DNAs = forensics.DNAs.ToList();
            component.Residues = forensics.Residues.ToList();
        }
        else
        {
            component.Fingerprints = [];
            component.Fibers = [];
            component.DNAs = [];
            component.Residues = [];
        }

        if (_tag.HasTag(target, DnaSolutionScannableTag))
            component.DNAs.AddRange(_forensicsSystem.GetSolutionsDNA(target));

        Dirty(scanner);
    }
}
