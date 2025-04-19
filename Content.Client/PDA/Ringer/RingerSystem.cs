using Content.Shared.PDA;
using Content.Shared.PDA.Ringer;
using Content.Shared.Store.Components;

namespace Content.Client.PDA.Ringer;

/// <summary>
/// Handles the client-side logic for <see cref="SharedRingerSystem"/>.
/// </summary>
public sealed class RingerSystem : SharedRingerSystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RingerComponent, AfterAutoHandleStateEvent>(OnRingerUpdate);
    }

    /// <summary>
    /// Updates the UI whenever we get a new component state from the server.
    /// </summary>
    private void OnRingerUpdate(Entity<RingerComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateRingerUi(ent);
    }

    /// <inheritdoc/>
    protected override void UpdateRingerUi(Entity<RingerComponent> ent)
    {
        if (UI.TryGetOpenUi(ent.Owner, RingerUiKey.Key, out var bui))
        {
            bui.Update();
        }
    }

    /// <inheritdoc/>
    public override bool TryToggleUplink(EntityUid uid, Note[] ringtone, EntityUid? user = null)
    {
        if (!TryComp<RingerUplinkComponent>(uid, out var uplink))
            return false;

        if (!HasComp<StoreComponent>(uid))
            return false;

        // Special case for client-side prediction:
        // Since we can't expose the uplink code to clients for security reasons,
        // we assume if an antagonist is trying to set a ringtone, it's to unlock the uplink.
        // The server will properly verify the code and correct if needed.
        if (IsAntagonist(user))
            return ToggleUplinkInternal((uid, uplink));

        // Non-antagonists never get to toggle the uplink on the client
        return false;
    }
}
