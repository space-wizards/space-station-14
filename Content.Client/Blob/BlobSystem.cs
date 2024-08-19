using Content.Shared.Alert.Components;
using Content.Shared.Blob;
using Content.Shared.Blob.Components;

namespace Content.Client.Blob;

/// <inheritdoc/>
public sealed class BlobSystem : SharedBlobSystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();


        SubscribeLocalEvent<BlobOvermindComponent, GetGenericAlertCounterAmountEvent>(OnGetAlertCounterAmount);
    }

    private void OnGetAlertCounterAmount(Entity<BlobOvermindComponent> ent, ref GetGenericAlertCounterAmountEvent args)
    {
        if (args.Handled)
            return;

        args.Amount = ent.Comp.Resource;
    }
}
