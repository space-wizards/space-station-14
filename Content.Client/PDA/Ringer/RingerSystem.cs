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
}
