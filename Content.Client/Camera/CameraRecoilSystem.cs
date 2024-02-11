using System.Numerics;
using Content.Shared.Camera;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;

namespace Content.Client.Camera;

public sealed class CameraRecoilSystem : SharedCameraRecoilSystem
{
    [Dependency] private readonly IConfigurationManager _configManager = default!;

    protected float Intensity;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<CameraKickEvent>(OnCameraKick);

        _configManager.OnValueChanged(CCVars.ScreenShakeIntensity, OnCvarChanged, true);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _configManager.UnsubValueChanged(CCVars.ScreenShakeIntensity, OnCvarChanged);
    }

    private void OnCvarChanged(float value)
    {
        Intensity = value;
    }

    private void OnCameraKick(CameraKickEvent ev)
    {
        KickCamera(GetEntity(ev.NetEntity), ev.Recoil);
    }

    public override void KickCamera(EntityUid uid, Vector2 recoil, CameraRecoilComponent? component = null)
    {
        if (Intensity == 0)
            return;

        if (!Resolve(uid, ref component, false))
            return;

        recoil *= Intensity;

        // Use really bad math to "dampen" kicks when we're already kicked.
        var existing = component.CurrentKick.Length();
        var dampen = existing / KickMagnitudeMax;
        component.CurrentKick += recoil * (1 - dampen);

        if (component.CurrentKick.Length() > KickMagnitudeMax)
            component.CurrentKick = component.CurrentKick.Normalized() * KickMagnitudeMax;

        component.LastKickTime = 0;
    }
}
