using Content.Shared.Backmen.Blob.Components;
using Content.Shared.Popups;
using Robust.Shared.Timing;

namespace Content.Shared.Backmen.Blob;

public abstract class SharedBlobCarrierSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var blobFactoryQuery = EntityQueryEnumerator<BlobCarrierComponent>();
        while (blobFactoryQuery.MoveNext(out var ent, out var comp))
        {
            if (!comp.HasMind)
                return;

            comp.TransformationTimer += frameTime;

            if (_gameTiming.CurTime < comp.NextAlert)
                continue;

            var remainingTime = Math.Round(comp.TransformationDelay - comp.TransformationTimer, 0);
            _popup.PopupClient(Loc.GetString("carrier-blob-alert", ("second", remainingTime)), ent, ent, PopupType.LargeCaution);

            comp.NextAlert = _gameTiming.CurTime + TimeSpan.FromSeconds(comp.AlertInterval);

            if (!(comp.TransformationTimer >= comp.TransformationDelay))
                continue;

            TransformToBlob((ent, comp));
        }
    }

    protected abstract void TransformToBlob(Entity<BlobCarrierComponent> ent);
}
