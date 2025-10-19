// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.Serialization;

namespace Content.Shared.Mind;

[Serializable, NetSerializable]
public enum ToggleableGhostRoleVisuals : byte
{
    Status
}

[Serializable, NetSerializable]
public enum ToggleableGhostRoleStatus : byte
{
    Off,
    Searching,
    On
}
