// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using System.Numerics;
using Content.Shared.Camera;
using Robust.Shared.Player;

namespace Content.Server.Camera;

public sealed class CameraRecoilSystem : SharedCameraRecoilSystem
{
    public override void KickCamera(EntityUid euid, Vector2 kickback, CameraRecoilComponent? component = null)
    {
        if (!Resolve(euid, ref component, false))
            return;

        RaiseNetworkEvent(new CameraKickEvent(GetNetEntity(euid), kickback), euid);
    }
}
