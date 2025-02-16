using Content.Server.Arcade.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Arcade.EntitySystems;

/// <summary>
///
/// </summary>
public sealed class ArcadeSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;

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

    public void PlayWinSound(EntityUid uid, ArcadeComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        _audioSystem.PlayPvs(component.WinSound, uid);
    }

    public void PlayLossSound(EntityUid uid, ArcadeComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        _audioSystem.PlayPvs(component.LossSound, uid);
    }

    public void PlayNewGameSound(EntityUid uid, ArcadeComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        _audioSystem.PlayPvs(component.NewGameSound, uid);
    }
}
