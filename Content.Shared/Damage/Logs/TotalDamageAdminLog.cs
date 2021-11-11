using System;
using System.Text.Json.Serialization;
using Content.Shared.FixedPoint;
using Robust.Shared.GameObjects;

namespace Content.Shared.Damage.Logs;

public readonly record struct TotalDamageAdminLog(
    EntityUid Owner,
    int PreviousRawValue,
    int NewRawValue,
    int Shift = FixedPoint2.Shift)
{
    [JsonIgnore]
    public FixedPoint2 PreviousValue => FixedPoint2.New(PreviousRawValue);

    [JsonIgnore]
    public FixedPoint2 NewValue => FixedPoint2.New(NewRawValue);
}
