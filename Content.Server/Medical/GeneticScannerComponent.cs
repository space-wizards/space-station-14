using System;
using Content.Shared.DragDrop;
using Content.Shared.GeneticScanner;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Content.Server.Medical.GeneticScanner
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedGeneticScannerComponent))]
    public class GeneticScannerComponent : SharedGeneticScannerComponent
    {
        public static readonly TimeSpan InternalOpenAttemptDelay = TimeSpan.FromSeconds(0.5);
        public TimeSpan LastInternalOpenAttempt;

        public ContainerSlot BodyContainer = default!;

        // ECS this out!, when DragDropSystem and InteractionSystem refactored
        public override bool DragDropOn(DragDropEvent eventArgs)
        {
            return true;
        }
    }
}
