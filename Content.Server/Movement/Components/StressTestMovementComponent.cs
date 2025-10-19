// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using System.Numerics;

namespace Content.Server.Movement.Components;

[RegisterComponent]
public sealed partial class StressTestMovementComponent : Component
{
    public float Progress { get; set; }
    public Vector2 Origin { get; set; }
}
