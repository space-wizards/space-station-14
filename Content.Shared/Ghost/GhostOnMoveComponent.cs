// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.GameStates;

namespace Content.Shared.Ghost;

[RegisterComponent, NetworkedComponent]
public sealed partial class GhostOnMoveComponent : Component
{
    [DataField]
    public bool CanReturn = true;

    [DataField]
    public bool MustBeDead;
}
