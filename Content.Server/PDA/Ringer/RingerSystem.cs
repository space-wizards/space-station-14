using Content.Server.Store.Systems;
using Content.Shared.PDA;
using Content.Shared.PDA.Ringer;

namespace Content.Server.PDA.Ringer;

/// <summary>
/// Handles the server-side logic for <see cref="SharedRingerSystem"/>.
/// </summary>
public sealed class RingerSystem : SharedRingerSystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RingerComponent, CurrencyInsertAttemptEvent>(OnCurrencyInsert);
    }

    /// <summary>
    /// Handles the <see cref="CurrencyInsertAttemptEvent"/> for <see cref="RingerUplinkComponent"/>.
    /// </summary>
    private void OnCurrencyInsert(Entity<RingerComponent> ent, ref CurrencyInsertAttemptEvent args)
    {
        // TODO: Store isn't predicted, can't move it to shared
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
