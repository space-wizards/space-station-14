using System;
using Robust.Shared.Serialization;

namespace Content.Shared.Station;

[NetSerializable, Serializable]
public readonly record struct StationId(uint Id)
{
    public static StationId Invalid => new(0);
}
