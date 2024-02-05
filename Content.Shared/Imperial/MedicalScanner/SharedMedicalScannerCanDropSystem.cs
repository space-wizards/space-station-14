using Content.Shared.DragDrop;

namespace Content.Shared.Imperial.MedicalScanner;

public abstract class SharedMedicalScannerCanDropSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MedicalScannerCanDropComponent, CanDropTargetEvent>(CanDragDrop);
    }

    public void CanDragDrop(EntityUid uid, MedicalScannerCanDropComponent comp, ref CanDropTargetEvent args)
    {
        args.Handled = true;
        args.CanDrop = true;
    }
}
