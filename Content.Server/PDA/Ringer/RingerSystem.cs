using Content.Server.Store.Systems;
using Content.Shared.PDA;
using Content.Shared.PDA.Ringer;

namespace Content.Server.PDA.Ringer;

public sealed class RingerSystem : SharedRingerSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RingerComponent, CurrencyInsertAttemptEvent>(OnCurrencyInsert);
    }

    private void OnCurrencyInsert(Entity<RingerComponent> ent, ref CurrencyInsertAttemptEvent args)
    {
        // TOOD: Store isn't predicted, can't move it to shared
        if (!TryComp<RingerUplinkComponent>(ent, out var uplink))
        {
            args.Cancel();
            return;
        }

        // if the store can be locked, it must be unlocked first before inserting currency. Stops traitor checking.
        if (!uplink.Unlocked)
            args.Cancel();
    }
}
