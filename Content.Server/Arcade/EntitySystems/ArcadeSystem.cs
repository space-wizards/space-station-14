using Content.Server.Arcade.Components;
using Robust.Shared.Audio;

namespace Content.Server.Arcade.EntitySystems;

/// <summary>
///
/// </summary>
public sealed class ArcadeSystem : EntitySystem
{
    public void SetPlayer(EntityUid uid, EntityUid? player, ArcadeComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.Player = player;
    }

    public EntityUid? GetPlayer(EntityUid uid, ArcadeComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return null;

        return component.Player;
    }

    public SoundSpecifier? GetWinSound(EntityUid uid, ArcadeComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return null;

        return component.WinSound;
    }

    public SoundSpecifier? GetLossSound(EntityUid uid, ArcadeComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return null;

        return component.LossSound;
    }
}
