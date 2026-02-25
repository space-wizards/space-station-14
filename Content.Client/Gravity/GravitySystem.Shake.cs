using System.Numerics;
using Content.Shared.Camera;
using Content.Shared.Gravity;
using Robust.Client.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Client.Gravity;

public sealed partial class GravitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedCameraRecoilSystem _sharedCameraRecoil = default!;

    protected override void ShakeGrid(EntityUid uid, GravityComponent? gravity = null)
    {
        base.ShakeGrid(uid, gravity);

        if (!Resolve(uid, ref gravity) || !Timing.IsFirstTimePredicted)
            return;

        var localPlayer = _playerManager.LocalEntity;

        if (!TryComp(localPlayer, out TransformComponent? xform))
            return;

        if (xform.GridUid != uid ||
            xform.GridUid == null && xform.MapUid != uid)
        {
            return;
        }

        var kick = new Vector2(_random.NextFloat(), _random.NextFloat()) * GravityKick;
        _sharedCameraRecoil.KickCamera(localPlayer.Value, kick);

        // Determine location to play the sound within maximum audible range
        var randomOffset = _random.NextVector2(SharedAudioSystem.DefaultSoundRange);
        var audioCoords = Transform(localPlayer.Value).Coordinates.Offset(randomOffset);

        _audio.PlayStatic(gravity.Enabled ? gravity.GravityOn : gravity.GravityOff, localPlayer.Value, audioCoords, AudioParams.Default.WithVariation(0.125f));
    }
}
