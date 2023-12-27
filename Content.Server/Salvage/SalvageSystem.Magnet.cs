using Content.Server.Salvage.Magnet;

namespace Content.Server.Salvage;

public sealed partial class SalvageSystem
{
    private void InitializeMagnet()
    {
        SubscribeLocalEvent<SalvageMagnetDataComponent, MapInitEvent>(OnMagnetDataMapInit);
    }

    private void OnMagnetDataMapInit(EntityUid uid, SalvageMagnetDataComponent component, ref MapInitEvent args)
    {
        CreateOffers(component);
    }

    private void UpdateMagnet()
    {
        var dataQuery = EntityQueryEnumerator<SalvageMagnetDataComponent>();
        var curTime = _timing.CurTime;

        while (dataQuery.MoveNext(out var uid, out var magnetData))
        {
            // Magnet currently active.
            if (magnetData.EndTime != null)
            {
                if (magnetData.EndTime.Value < curTime)
                {
                    // TODO: Handle ending
                }
            }

            if (magnetData.NextOffer < curTime)
            {
                CreateOffers(magnetData);
            }
        }
    }

    private void CreateOffers(SalvageMagnetDataComponent data)
    {
        data.Offered.Clear();

        for (var i = 0; i < data.Offers; i++)
        {
            var seed = _random.Next();
            data.Offered.Add(seed);
        }

        // TODO: Update UIs
    }
}
