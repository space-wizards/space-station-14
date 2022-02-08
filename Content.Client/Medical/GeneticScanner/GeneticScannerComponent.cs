using Content.Shared.DragDrop;
using Content.Shared.Medical.GeneticScanner;

namespace Content.Client.Medical.GeneticScanner
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
