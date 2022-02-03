using Content.Server.Storage.Components;
using Content.Shared.Interaction;
using Robust.Shared.GameObjects;

namespace Content.Server.Medical.GeneticScanner
{
    [RegisterComponent]
    [ComponentReference(typeof(EntityStorageComponent))]
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(IStorageComponent))]
    public class GeneticScannerEntityStorageComponent : EntityStorageComponent
    {
        public override string Name => "GeneticScannerEntityStorage";
    }
}
