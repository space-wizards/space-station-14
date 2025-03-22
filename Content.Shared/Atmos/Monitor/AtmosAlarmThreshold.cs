using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Monitor;


[Prototype("alarmThreshold")]
public sealed partial class AtmosAlarmThresholdPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("ignore")]
    public bool Ignore;

    [DataField("upperBound")]
    public AlarmThresholdSetting UpperBound = AlarmThresholdSetting.Disabled;

    [DataField("lowerBound")]
    public AlarmThresholdSetting LowerBound = AlarmThresholdSetting.Disabled;

    [DataField("upperWarnAround")]
    public AlarmThresholdSetting UpperWarningPercentage = AlarmThresholdSetting.Disabled;

    [DataField("lowerWarnAround")]
    public AlarmThresholdSetting LowerWarningPercentage = AlarmThresholdSetting.Disabled;
}

[Serializable, NetSerializable, DataDefinition]
public sealed partial class AtmosAlarmThreshold
{
    [DataField("ignore")]
    public bool Ignore;

    [DataField("upperBound")]
    private AlarmThresholdSetting _upperBound = AlarmThresholdSetting.Disabled;

    [DataField("lowerBound")]
    private AlarmThresholdSetting _lowerBound = AlarmThresholdSetting.Disabled;

    [DataField("upperWarnAround")]
    public AlarmThresholdSetting UpperWarningPercentage = AlarmThresholdSetting.Disabled;

    [DataField("lowerWarnAround")]
    public AlarmThresholdSetting LowerWarningPercentage = AlarmThresholdSetting.Disabled;

    public AlarmThresholdSetting UpperBound
    {
        get => _upperBound;
        set
        {
            // Because the warnings are stored as percentages of the bounds,
            // Make a copy of the calculated bounds, so that the real warning amount
            // doesn't change value when user changes the bounds
            var oldWarning = UpperWarningBound;
            _upperBound = value;
            UpperWarningBound = oldWarning;
        }
    }

    public AlarmThresholdSetting LowerBound
    {
        get => _lowerBound;
        set
        {
            // Because the warnings are stored as percentages of the bounds,
            // Make a copy of the calculated bounds, so that the real warning amount
            // doesn't change value when user changes the bounds
            var oldWarning = LowerWarningBound;
            _lowerBound = value;
            LowerWarningBound = oldWarning;
        }
    }

    [ViewVariables]
    public AlarmThresholdSetting UpperWarningBound
    {
        get => CalculateWarningBound(AtmosMonitorThresholdBound.Upper);
        set => UpperWarningPercentage = CalculateWarningPercentage(AtmosMonitorThresholdBound.Upper, value);
    }

    [ViewVariables]
    public AlarmThresholdSetting LowerWarningBound
    {
        get => CalculateWarningBound(AtmosMonitorThresholdBound.Lower);
        set => LowerWarningPercentage = CalculateWarningPercentage(AtmosMonitorThresholdBound.Lower, value);
    }

    public AtmosAlarmThreshold()
    {
    }

    public AtmosAlarmThreshold(AtmosAlarmThreshold other)
    {
        Ignore = other.Ignore;
        UpperBound = other.UpperBound;
        LowerBound = other.LowerBound;
        UpperWarningPercentage = other.UpperWarningPercentage;
        LowerWarningPercentage = other.LowerWarningPercentage;
    }

    public AtmosAlarmThreshold(AtmosAlarmThresholdPrototype proto)
    {
        Ignore = proto.Ignore;
        UpperBound = proto.UpperBound;
        LowerBound = proto.LowerBound;
        UpperWarningPercentage = proto.UpperWarningPercentage;
        LowerWarningPercentage = proto.LowerWarningPercentage;
    }

    // utility function to check a threshold against some calculated value
    public bool CheckThreshold(float value, out AtmosAlarmType state)
    {
        return CheckThreshold(value, out state, out AtmosMonitorThresholdBound _);
    }

    // utility function to check a threshold against some calculated value. If the output state
    // is normal, whichFailed should not be used..
    public bool CheckThreshold(float value, out AtmosAlarmType state, out AtmosMonitorThresholdBound whichFailed)
    {
        state = AtmosAlarmType.Normal;
        whichFailed = AtmosMonitorThresholdBound.Upper;

        if (Ignore)
        {
            return false;
        }

        if (value >= UpperBound)
        {
            state = AtmosAlarmType.Danger;
            whichFailed = AtmosMonitorThresholdBound.Upper;
            return true;
        }
        if(value <= LowerBound)
        {
            state = AtmosAlarmType.Danger;
            whichFailed = AtmosMonitorThresholdBound.Lower;
            return true;
        }
        if (value >= UpperWarningBound)
        {
            state = AtmosAlarmType.Warning;
            whichFailed = AtmosMonitorThresholdBound.Upper;
            return true;
        }
        if (value <= LowerWarningBound)
        {
            state = AtmosAlarmType.Warning;
            whichFailed = AtmosMonitorThresholdBound.Lower;
            return true;
        }

        return false;
    }

    /// Warnings are stored in prototypes as a percentage, for ease of content
    /// maintainers. This recalculates a new "real" value of the warning
    /// threshold, for use in the actual atmosphereic checks.
    public AlarmThresholdSetting CalculateWarningBound(AtmosMonitorThresholdBound bound)
    {
        switch (bound)
        {
            case AtmosMonitorThresholdBound.Upper:
                return new AlarmThresholdSetting {
                    Enabled = UpperWarningPercentage.Enabled,
                    Value = UpperBound.Value * UpperWarningPercentage.Value};
            case AtmosMonitorThresholdBound.Lower:
                return new AlarmThresholdSetting {
                    Enabled = LowerWarningPercentage.Enabled,
                    Value = LowerBound.Value * LowerWarningPercentage.Value};
            default:
                // Unreachable.
                return new AlarmThresholdSetting();
        }
    }

    public AlarmThresholdSetting CalculateWarningPercentage(AtmosMonitorThresholdBound bound, AlarmThresholdSetting warningBound)
    {
        switch (bound)
        {
            case AtmosMonitorThresholdBound.Upper:
                return new AlarmThresholdSetting {
                    Enabled = UpperWarningPercentage.Enabled,
                    Value = UpperBound.Value == 0 ? 0 : warningBound.Value / UpperBound.Value};
            case AtmosMonitorThresholdBound.Lower:
                return new AlarmThresholdSetting {
                    Enabled = LowerWarningPercentage.Enabled,
                    Value = LowerBound.Value == 0 ? 0 : warningBound.Value / LowerBound.Value };
            default:
                // Unreachable.
                return new AlarmThresholdSetting();
        }
    }

    // Enable or disable a single threshold setting
    public void SetEnabled(AtmosMonitorLimitType whichLimit, bool isEnabled)
    {
        switch(whichLimit)
        {
            case AtmosMonitorLimitType.LowerDanger:
                LowerBound = LowerBound.WithEnabled(isEnabled);
                break;
            case AtmosMonitorLimitType.LowerWarning:
                LowerWarningPercentage = LowerWarningPercentage.WithEnabled(isEnabled);
                break;
            case AtmosMonitorLimitType.UpperWarning:
                UpperWarningPercentage = UpperWarningPercentage.WithEnabled(isEnabled);
                break;
            case AtmosMonitorLimitType.UpperDanger:
                UpperBound = UpperBound.WithEnabled(isEnabled);
                break;
        }
    }

    // Set the limit for a threshold. Will clamp other limits appropriately to
    // enforce that LowerBound <= LowerWarningBound <= UpperWarningBound <= UpperBound
    public void SetLimit(AtmosMonitorLimitType whichLimit, float limit)
    {
        if (limit <= 0)
        {
            // Unit tests expect that setting value of 0 or less should not change the limit.
            // Feels a bit strange, but does avoid a bug where the warning data (stored as a
            // percentage of danger bounds) is lost when setting the danger threshold to zero
            return;
        }

        switch (whichLimit)
        {
            case AtmosMonitorLimitType.LowerDanger:
                LowerBound = LowerBound.WithThreshold(limit);
                LowerWarningBound = LowerWarningBound.WithThreshold(Math.Max(limit, LowerWarningBound.Value));
                UpperWarningBound = UpperWarningBound.WithThreshold(Math.Max(limit, UpperWarningBound.Value));
                UpperBound = UpperBound.WithThreshold(Math.Max(limit, UpperBound.Value));
                break;
            case AtmosMonitorLimitType.LowerWarning:
                LowerBound = LowerBound.WithThreshold(Math.Min(LowerBound.Value, limit));
                LowerWarningBound = LowerWarningBound.WithThreshold(limit);
                UpperWarningBound = UpperWarningBound.WithThreshold(Math.Max(limit, UpperWarningBound.Value));
                UpperBound = UpperBound.WithThreshold(Math.Max(limit, UpperBound.Value));
                break;
            case AtmosMonitorLimitType.UpperWarning:
                LowerBound = LowerBound.WithThreshold(Math.Min(LowerBound.Value, limit));
                LowerWarningBound = LowerWarningBound.WithThreshold(Math.Min(LowerWarningBound.Value, limit));
                UpperWarningBound = UpperWarningBound.WithThreshold(limit);
                UpperBound = UpperBound.WithThreshold(Math.Max(limit, UpperBound.Value));
                break;
            case AtmosMonitorLimitType.UpperDanger:
                LowerBound = LowerBound.WithThreshold(Math.Min(LowerBound.Value, limit));
                LowerWarningBound = LowerWarningBound.WithThreshold(Math.Min(LowerWarningBound.Value, limit));
                UpperWarningBound = UpperWarningBound.WithThreshold(Math.Min(UpperWarningBound.Value, limit));
                UpperBound = UpperBound.WithThreshold(limit);
                break;
        }
    }

    /// <summary>
    ///     Iterates through the changes that these threshold settings would make from a
    ///     previous instance. Basically, diffs the two settings.
    /// </summary>
    public IEnumerable<AtmosAlarmThresholdChange> GetChanges(AtmosAlarmThreshold previous)
    {
        if (LowerBound != previous.LowerBound)
            yield return new AtmosAlarmThresholdChange(AtmosMonitorLimitType.LowerDanger, previous.LowerBound, LowerBound);

        if (LowerWarningBound != previous.LowerWarningBound)
            yield return new AtmosAlarmThresholdChange(AtmosMonitorLimitType.LowerWarning, previous.LowerWarningBound, LowerWarningBound);

        if (UpperBound != previous.UpperBound)
            yield return new AtmosAlarmThresholdChange(AtmosMonitorLimitType.UpperDanger, previous.UpperBound, UpperBound);

        if (UpperWarningBound != previous.UpperWarningBound)
            yield return new AtmosAlarmThresholdChange(AtmosMonitorLimitType.UpperWarning, previous.UpperWarningBound, UpperWarningBound);
    }
}

/// <summary>
///     A change of a single value between two AtmosAlarmThreshold, for a given AtmosMonitorLimitType
/// </summary>
public readonly struct AtmosAlarmThresholdChange
{
    /// <summary>
    ///     The type of change between the two threshold sets
    /// </summary>
    public readonly AtmosMonitorLimitType Type;

    /// <summary>
    ///     The value in the old threshold set
    /// </summary>
    public readonly AlarmThresholdSetting? Previous;

    /// <summary>
    ///     The value in the new threshold set
    /// </summary>
    public readonly AlarmThresholdSetting Current;

    public AtmosAlarmThresholdChange(AtmosMonitorLimitType type, AlarmThresholdSetting? previous, AlarmThresholdSetting current)
    {
        Type = type;
        Previous = previous;
        Current = current;
    }
}

[DataDefinition, Serializable]
public readonly partial struct AlarmThresholdSetting: IEquatable<AlarmThresholdSetting>
{
    [DataField("enabled")]
    public bool Enabled { get; init; } = true;

    [DataField("threshold")]
    public float Value { get; init; } = 1;

    public static AlarmThresholdSetting Disabled = new() {Enabled = false, Value = 0};

    public AlarmThresholdSetting()
    {
    }

    public static bool operator <=(float a, AlarmThresholdSetting b)
    {
        return b.Enabled && a <= b.Value;
    }

    public static bool operator >=(float a, AlarmThresholdSetting b)
    {
        return b.Enabled && a >= b.Value;
    }

    public AlarmThresholdSetting WithThreshold(float threshold)
    {
        return this with {Value = threshold};
    }

    public AlarmThresholdSetting WithEnabled(bool enabled)
    {
        return this with {Enabled = enabled};
    }

    public bool Equals(AlarmThresholdSetting other)
    {
        if (Enabled != other.Enabled)
            return false;

        if (Value != other.Value)
            return false;

        return true;
    }

    public static bool operator ==(AlarmThresholdSetting lhs, AlarmThresholdSetting rhs)
    {
        return lhs.Equals(rhs);
    }

    public static bool operator !=(AlarmThresholdSetting lhs, AlarmThresholdSetting rhs)
    {
        return !lhs.Equals(rhs);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Enabled, Value);
    }
}

public enum AtmosMonitorThresholdBound
{
    Upper,
    Lower
}

public enum AtmosMonitorLimitType //<todo.eoin Very similar to the above...
{
    LowerDanger,
    LowerWarning,
    UpperWarning,
    UpperDanger,
}

// not really used in the prototype but in code,
// to differentiate between the different
// fields you can find this prototype in
public enum AtmosMonitorThresholdType
{
    Temperature,
    Pressure,
    Gas
}

[Serializable, NetSerializable]
public enum AtmosMonitorVisuals : byte
{
    AlarmType,
}
