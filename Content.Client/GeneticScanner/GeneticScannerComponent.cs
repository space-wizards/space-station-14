using Content.Shared.DragDrop;
using Content.Shared.GeneticScanner;
using Robust.Shared.GameObjects;

namespace Content.Client.GeneticScanner
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedGeneticScannerComponent))]
    public sealed class GeneticScannerComponent : SharedGeneticScannerComponent
    {
        public override bool DragDropOn(DragDropEvent eventArgs)
        {
            return false;
        }
    }
}
