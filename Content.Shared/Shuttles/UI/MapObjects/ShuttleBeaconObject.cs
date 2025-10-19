// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Shuttles.UI.MapObjects;

[Serializable, NetSerializable]
public readonly record struct ShuttleBeaconObject(NetEntity Entity, NetCoordinates Coordinates, string Name) : IMapObject
{
    public bool HideButton => false;
}
