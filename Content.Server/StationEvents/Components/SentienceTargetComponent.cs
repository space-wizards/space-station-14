// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Content.Server.StationEvents.Events;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(RandomSentienceRule))]
public sealed partial class SentienceTargetComponent : Component
{
    [DataField(required: true)]
    public string FlavorKind = default!;

    [DataField]
    public float Weight = 1.0f;
}
