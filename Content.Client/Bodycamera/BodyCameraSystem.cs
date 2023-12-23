using Content.Shared.Bodycamera;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Client.Bodycamera;

public sealed class BodyCameraSystem : SharedBodyCameraSystem
{
    public override void Initialize()
    {
        base.Initialize();
    }

    /// <summary>
    /// Enable the camera and play sound predicted
    /// </summary>
    protected override bool TryEnable(EntityUid uid, BodyCameraComponent comp)
    {
        if (!base.TryEnable(uid, comp))
            return false;

        _audio.PlayPredicted(comp.PowerOnSound, uid, null);
        return true;
    }

    /// <summary>
    /// Disable the camera and play sound predicted
    /// </summary>
    protected override bool TryDisable(EntityUid uid, BodyCameraComponent comp)
    {
        if (!base.TryDisable(uid, comp))
            return false;

        _audio.PlayPredicted(comp.PowerOffSound, uid, null);
        return true;
    }
}
