using System;
using Content.Shared.Atmos.Monitor;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Atmos.Monitor
{
    // mostly based around floats and percentages, no literals
    // except for the range boundaries
    [Prototype("alarmThreshold")]
    [Serializable, NetSerializable]
    public class AtmosAlarmThreshold : IPrototype, ISerializationHooks
    {
        [DataField("id", required: true)]
        public string ID { get; } = default!;
        [ViewVariables]
        [DataField("ignore")]
        public bool Ignore = false;

        // zero bounds are not allowed - just
        // set the bound to null if you want
        // to disable it
        [ViewVariables]
        [DataField("upperBound")]
        public float? UpperBound { get; private set; }

        [ViewVariables]
        [DataField("lowerBound")]
        public float? LowerBound { get; private set; }

        // upper warning percentage
        // must always cause UpperWarningBound
        // to be smaller
        [ViewVariables]
        [DataField("upperWarnAround")]
        public float? UpperWarningPercentage { get; private set; }

        // lower warning percentage
        // must always cause LowerWarningBound
        // to be larger
        [ViewVariables]
        [DataField("lowerWarnAround")]
        public float? LowerWarningPercentage { get; private set; }

        [ViewVariables]
        public float? UpperWarningBound
        {
            get => CalculateWarningBound(AtmosMonitorThresholdBound.Upper);
        }

        [ViewVariables]
        public float? LowerWarningBound
        {
            get => CalculateWarningBound(AtmosMonitorThresholdBound.Lower);
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
        public bool CheckThreshold(float value, out AtmosMonitorAlarmType state)
        {
            state = AtmosMonitorAlarmType.Normal;
            if (Ignore) return false;

            if (value >= UpperBound || value <= LowerBound)
            {
                state = AtmosMonitorAlarmType.Danger;
                return true;
            }
            if (value >= UpperWarningBound || value <= LowerWarningBound)
            {
                state = AtmosMonitorAlarmType.Warning;
                return true;
            }

            return false;
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

            float value = (float) input;

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

            bool isValid = true;
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

                    float upperWarning = (float) (input / UpperBound);
                    float upperTestValue = (upperWarning * (float) UpperBound);

                    if (upperWarning > 1f
                        || upperTestValue < LowerWarningBound
                        || upperTestValue < LowerBound)
                        return false;

                    UpperWarningPercentage = upperWarning;

                    return true;
                case AtmosMonitorThresholdBound.Lower:
                    if (LowerBound == null)
                        return false;

                    float lowerWarning = (float) (input / LowerBound);
                    float testValue = (lowerWarning * (float) LowerBound);

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
}
