using Content.Server.Storage.Components;
using Robust.Shared.GameObjects;

namespace Content.Server.Medical.GeneticScanner
{
    [RegisterComponent]
    public class GeneticScannerEntityStorageComponent : EntityStorageComponent
    {
        public override string Name => "GeneticScannerEntityStorage";
    }
}
