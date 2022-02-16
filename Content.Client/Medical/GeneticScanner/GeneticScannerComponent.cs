using Content.Shared.DragDrop;
using Content.Shared.Cloning.GeneticScanner;

namespace Content.Client.Cloning.GeneticScanner
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
