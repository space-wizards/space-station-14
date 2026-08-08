using Content.Shared.Forensics.Components;
using Content.Shared.Forensics.Events;
using Content.Shared.Forensics.Systems;

namespace Content.Client.Forensics.Systems;

/// <summary>
/// This is solely existent for client-side prediction when cleaning items.
/// </summary>
public sealed partial class ForensicsSystem : SharedForensicsSystem
{

    /// <summary>
    /// The Client needs to set the prediction boolean "IsDirty" to false upon a successful cleaning,
    /// so it cannot mispredict when someone tries to clean it again before the Dirty() from the server comes in.
    /// </summary>
    protected override void OnCleanForensicsDoAfter(Entity<ForensicsComponent> component, ref CleanForensicsDoAfterEvent args)
    {
        if (args.Handled
            || args.Cancelled
            || args.Target == null
            || !TryComp<ForensicsComponent>(args.Target, out var targetComp))
            return;

        targetComp.IsDirty = false;
        Dirty(args.Target.Value, targetComp);
    }
}
