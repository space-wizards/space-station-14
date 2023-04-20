using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Monitor;

// mostly based around floats and percentages, no literals
// except for the range boundaries
[Prototype("alarmThreshold")]
[Serializable, NetSerializable]
public sealed class AtmosAlarmThreshold : IPrototype, ISerializationHooks
{
    [IdDataField]
    public string ID { get; } = default!;
    [DataField("ignore")]
    public bool Ignore;

    [DataField("upperBound")]
    private AlarmThresholdSetting _UpperBound;

    public AlarmThresholdSetting UpperBound { get { return _UpperBound; } private set
        {
            // Because the warnings are stored as percentages of the bounds,
            // Make a copy of the calculated bounds, so that the real warning amount
            // doesn't change value when user changes the bounds
            var oldWarning = UpperWarningBound;
            _UpperBound = value;
            UpperWarningBound = oldWarning;
        }
    }

    [DataField("lowerBound")]
    public AlarmThresholdSetting _LowerBound;

    public AlarmThresholdSetting LowerBound { get { return _LowerBound; } private set
        {
            // Because the warnings are stored as percentages of the bounds,
            // Make a copy of the calculated bounds, so that the real warning amount
            // doesn't change value when user changes the bounds
            var oldWarning = LowerWarningBound;
            _LowerBound = value;
            LowerWarningBound = oldWarning;
        }
    }

    // upper warning percentage
    // must always cause UpperWarningBound
    // to be smaller
    [DataField("upperWarnAround")]
    public AlarmThresholdSetting UpperWarningPercentage { get; private set; }

    // lower warning percentage
    // must always cause LowerWarningBound
    // to be larger
    [DataField("lowerWarnAround")]
    public AlarmThresholdSetting LowerWarningPercentage { get; private set; }

    [ViewVariables]
    public AlarmThresholdSetting UpperWarningBound
    {
        get { return CalculateWarningBound(AtmosMonitorThresholdBound.Upper); }
        set { UpperWarningPercentage = CalculateWarningPercentage(AtmosMonitorThresholdBound.Upper, value); }
    }

    [ViewVariables]
    public AlarmThresholdSetting LowerWarningBound
    {
        get { return CalculateWarningBound(AtmosMonitorThresholdBound.Lower); }
        set { LowerWarningPercentage = CalculateWarningPercentage(AtmosMonitorThresholdBound.Lower, value); }
    }

    public AtmosAlarmThreshold()
    {
        UpperBound = new AlarmThresholdSetting();
        LowerBound = new AlarmThresholdSetting();
        UpperWarningPercentage = new AlarmThresholdSetting();
        LowerWarningPercentage = new AlarmThresholdSetting();
    }

    public AtmosAlarmThreshold(AtmosAlarmThreshold other)
    {
        Ignore = other.Ignore;
        UpperBound = other.UpperBound;
        LowerBound = other.LowerBound;
        UpperWarningPercentage = other.UpperWarningPercentage;
        LowerWarningPercentage = other.LowerWarningPercentage;
    }

    void ISerializationHooks.AfterDeserialization()
    {
        UpperBound = new AlarmThresholdSetting{ Enabled = UpperBound.Value != 0, Value = UpperBound.Value };
        LowerBound = new AlarmThresholdSetting{ Enabled = LowerBound.Value != 0, Value = LowerBound.Value };
        UpperWarningPercentage = new AlarmThresholdSetting{ Enabled = UpperWarningPercentage.Value != 0, Value = UpperWarningPercentage.Value };
        LowerWarningPercentage = new AlarmThresholdSetting{ Enabled = LowerWarningPercentage.Value != 0, Value = LowerWarningPercentage.Value };
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

    [DataDefinition, Serializable]
    public struct AlarmThresholdSetting
    {
        [DataField("enabled")]
        public bool Enabled { get; set; } = false;
        [DataField("threshold")]
        public float Value { get; set; } = 0;

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
            return new AlarmThresholdSetting{ Enabled = Enabled, Value = threshold };
        }

        public AlarmThresholdSetting WithEnabled(bool enabled)
        {
            return new AlarmThresholdSetting{ Enabled = enabled, Value = Value };
        }
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
