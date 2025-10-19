// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Content.Shared.SurveillanceCamera;

namespace Content.Client.SurveillanceCamera;

// Dummy component so that targetted events work on client for
// appearance events.
[RegisterComponent]
public sealed partial class SurveillanceCameraVisualsComponent : Component
{
    [DataField("sprites")]
    public Dictionary<SurveillanceCameraVisuals, string> CameraSprites = new();
}
