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

    // zero bounds are not allowed - just
    // set the bound to null if you want
    // to disable it
    [DataField("upperBound")]
    public AlarmThresholdSetting UpperBound { get; private set; }

    [DataField("lowerBound")]
    public AlarmThresholdSetting LowerBound { get; private set; }

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
    public AlarmThresholdSetting UpperWarningBound => CalculateWarningBound(AtmosMonitorThresholdBound.Upper);

    [ViewVariables]
    public AlarmThresholdSetting LowerWarningBound => CalculateWarningBound(AtmosMonitorThresholdBound.Lower);

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
        TrySetWarningBound(AtmosMonitorThresholdBound.Upper, UpperBound.Value * UpperWarningPercentage.Value);
        TrySetWarningBound(AtmosMonitorThresholdBound.Lower, LowerBound.Value * LowerWarningPercentage.Value);
    }

    // utility function to check a threshold against some calculated value
    public bool CheckThreshold(float value, out AtmosAlarmType state)
    {
        state = AtmosAlarmType.Normal;
        if (Ignore)
        {
            return false;
        }

        if (value >= UpperBound || value <= LowerBound.Value)
        {
            state = AtmosAlarmType.Danger;
            return true;
        }
        if (value >= UpperWarningBound || value <= LowerWarningBound.Value)
        {
            state = AtmosAlarmType.Warning;
            return true;
        }

        return true;
    }

    // set the primary bound, takes a hard value
    public bool TrySetPrimaryBound(AtmosMonitorThresholdBound bound, float? input)
    {
        if (input == null)
        {
            switch (bound)
            {
                case AtmosMonitorThresholdBound.Upper:
                    UpperBound = new AlarmThresholdSetting{ Enabled = false, Value = UpperBound.Value };
                    break;
                case AtmosMonitorThresholdBound.Lower:
                    LowerBound = new AlarmThresholdSetting{ Enabled = false, Value = LowerBound.Value };
                    break;
            }

            return true;
        }

        var value = (float) input;

        if (value <= 0f || float.IsNaN(value))
            return false;

        (float target, int compare)? targetValue = null;
        switch (bound)
        {
            case AtmosMonitorThresholdBound.Upper:
                if (float.IsPositiveInfinity(value))
                    return false;

                if (LowerBound.Enabled)
                    targetValue = ((float) LowerBound.Value, -1);
                break;
            case AtmosMonitorThresholdBound.Lower:
                if (float.IsNegativeInfinity(value))
                    return false;

                if (UpperBound.Enabled)
                    targetValue = ((float) UpperBound.Value, 1);
                break;
        }

        var isValid = true;
        if (targetValue != null)
        {
            var result = targetValue.Value.target.CompareTo(value);
            isValid = targetValue.Value.compare == result;
        }

        if (isValid)
        {
            switch (bound)
            {
                case AtmosMonitorThresholdBound.Upper:
                    UpperBound = new AlarmThresholdSetting{Enabled = true, Value = value};
                    return true;
                case AtmosMonitorThresholdBound.Lower:
                    LowerBound = new AlarmThresholdSetting{Enabled = true, Value = value};
                    return true;
            }
        }

        return false;
    }

    // set the warning bound, takes a hard value
    //
    // this will always set the percentage and
    // the raw value at the same time
    public bool TrySetWarningBound(AtmosMonitorThresholdBound bound, float? input)
    {
        if (input == null)
        {
            switch (bound)
            {
                case AtmosMonitorThresholdBound.Upper:
                    UpperWarningPercentage = new AlarmThresholdSetting{Enabled = false, Value = UpperWarningPercentage.Value};
                    break;
                case AtmosMonitorThresholdBound.Lower:
                    LowerWarningPercentage = new AlarmThresholdSetting{Enabled = false, Value = LowerWarningPercentage.Value};
                    break;
            }

            return true;
        }

        switch (bound)
        {
            case AtmosMonitorThresholdBound.Upper:
                if (!UpperBound.Enabled)
                    return false;

                var upperWarning = (float) (input / UpperBound.Value);
                var upperTestValue = upperWarning * (float) UpperBound.Value;

                if (upperWarning > 1f
                    || upperTestValue < LowerWarningBound.Value
                    || upperTestValue < LowerBound.Value)
                    return false;

                UpperWarningPercentage = new AlarmThresholdSetting{Enabled = true, Value = upperWarning};

                return true;
            case AtmosMonitorThresholdBound.Lower:
                if (!LowerBound.Enabled)
                    return false;

                var lowerWarning = (float) (input / LowerBound.Value);
                var testValue = lowerWarning * (float) LowerBound.Value;

                if (lowerWarning < 1f
                    || testValue > UpperWarningBound.Value
                    || testValue > UpperBound.Value)
                    return false;

                LowerWarningPercentage = new AlarmThresholdSetting{Enabled = true, Value = lowerWarning};

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
    }
}

public enum AtmosMonitorThresholdBound
{
    Upper,
    Lower
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
