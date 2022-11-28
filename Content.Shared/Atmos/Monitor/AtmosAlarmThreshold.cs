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
    public float? UpperBound { get; private set; }

    [DataField("lowerBound")]
    public float? LowerBound { get; private set; }

    // upper warning percentage
    // must always cause UpperWarningBound
    // to be smaller
    [DataField("upperWarnAround")]
    public float? UpperWarningPercentage { get; private set; }

    // lower warning percentage
    // must always cause LowerWarningBound
    // to be larger
    [DataField("lowerWarnAround")]
    public float? LowerWarningPercentage { get; private set; }

    [ViewVariables]
    public float? UpperWarningBound => CalculateWarningBound(AtmosMonitorThresholdBound.Upper);

    [ViewVariables]
    public float? LowerWarningBound => CalculateWarningBound(AtmosMonitorThresholdBound.Lower);

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

    void ISerializationHooks.AfterDeserialization()
    {
        if (UpperBound <= LowerBound)
            UpperBound = null;

        if (LowerBound >= UpperBound)
            LowerBound = null;

        if (UpperWarningPercentage != null)
            TrySetWarningBound(AtmosMonitorThresholdBound.Upper, UpperBound * UpperWarningPercentage);

        if (LowerWarningPercentage != null)
            TrySetWarningBound(AtmosMonitorThresholdBound.Lower, LowerBound * LowerWarningPercentage);
    }

    // utility function to check a threshold against some calculated value
    public bool CheckThreshold(float value, out AtmosAlarmType state)
    {
        state = AtmosAlarmType.Normal;
        if (Ignore)
        {
            return false;
        }

        if (value >= UpperBound || value <= LowerBound)
        {
            state = AtmosAlarmType.Danger;
            return true;
        }
        if (value >= UpperWarningBound || value <= LowerWarningBound)
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
                    UpperBound = null;
                    break;
                case AtmosMonitorThresholdBound.Lower:
                    LowerBound = null;
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

                if (LowerBound != null)
                    targetValue = ((float) LowerBound, -1);
                break;
            case AtmosMonitorThresholdBound.Lower:
                if (float.IsNegativeInfinity(value))
                    return false;

                if (UpperBound != null)
                    targetValue = ((float) UpperBound, 1);
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
                    UpperBound = value;
                    return true;
                case AtmosMonitorThresholdBound.Lower:
                    LowerBound = value;
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
                    UpperWarningPercentage = null;
                    break;
                case AtmosMonitorThresholdBound.Lower:
                    LowerWarningPercentage = null;
                    break;
            }

            return true;
        }

        switch (bound)
        {
            case AtmosMonitorThresholdBound.Upper:
                if (UpperBound == null)
                    return false;

                var upperWarning = (float) (input / UpperBound);
                var upperTestValue = upperWarning * (float) UpperBound;

                if (upperWarning > 1f
                    || upperTestValue < LowerWarningBound
                    || upperTestValue < LowerBound)
                    return false;

                UpperWarningPercentage = upperWarning;

                return true;
            case AtmosMonitorThresholdBound.Lower:
                if (LowerBound == null)
                    return false;

                var lowerWarning = (float) (input / LowerBound);
                var testValue = lowerWarning * (float) LowerBound;

                if (lowerWarning < 1f
                    || testValue > UpperWarningBound
                    || testValue > UpperBound)
                    return false;

                LowerWarningPercentage = lowerWarning;

                return true;
        }

        return false;
    }

    public float? CalculateWarningBound(AtmosMonitorThresholdBound bound)
    {
        float? value = null;

        switch (bound)
        {
            case AtmosMonitorThresholdBound.Upper:
                if (UpperBound == null || UpperWarningPercentage == null)
                    break;

                value = UpperBound * UpperWarningPercentage;
                break;
            case AtmosMonitorThresholdBound.Lower:
                if (LowerBound == null || LowerWarningPercentage == null)
                    break;

                value = LowerBound * LowerWarningPercentage;
                break;
        }

        return value;
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
