using System.Numerics;
using Content.Shared.Camera;
using Content.Shared.DeadSpace.NicotineAddiction;
using Robust.Client.Player;
using Robust.Shared.Random;

namespace Content.Client.DeadSpace.NicotineAddiction;

public sealed class NicotineDeprivationEffectsSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedCameraRecoilSystem _cameraRecoil = default!;

    private const float ScreenKick = 0.12f;
    private const float EyeNudge = 0.04f;
    private const float EffectInterval = 0.1f;
    private const float EyeSmoothingSpeed = 12f;

    private Vector2 _eyeNudge;
    private Vector2 _eyeNudgeTarget;
    private float _effectAccumulator = EffectInterval;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NicotineAddictionComponent, GetEyeOffsetEvent>(OnEyeOffset);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var local = _player.LocalEntity;
        if (local == null || !TryComp<NicotineAddictionComponent>(local, out var c) || !c.DeprivationShakeActive)
        {
            _eyeNudge = Vector2.Zero;
            _eyeNudgeTarget = Vector2.Zero;
            _effectAccumulator = EffectInterval;
            return;
        }

        _effectAccumulator += frameTime;
        if (_effectAccumulator >= EffectInterval)
        {
            _effectAccumulator %= EffectInterval;
            _cameraRecoil.KickCamera(local.Value,
                new Vector2(_random.NextFloat(-1f, 1f), _random.NextFloat(-1f, 1f)) * ScreenKick);

            _eyeNudgeTarget = new Vector2(_random.NextFloat(-1f, 1f), _random.NextFloat(-1f, 1f)) * EyeNudge;
        }

        var smoothing = Math.Clamp(frameTime * EyeSmoothingSpeed, 0f, 1f);
        _eyeNudge = Vector2.Lerp(_eyeNudge, _eyeNudgeTarget, smoothing);
    }

    private void OnEyeOffset(EntityUid uid, NicotineAddictionComponent comp, ref GetEyeOffsetEvent args)
    {
        if (!comp.DeprivationShakeActive || uid != _player.LocalEntity)
            return;
        args.Offset += _eyeNudge;
    }
}
