using System;
using Content.Shared.Atmos.Monitor;
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
    public class AtmosAlarmThreshold : IPrototype
    {
        [DataField("id", required: true)]
        public string ID { get; } = default!;
        [ViewVariables]
        [DataField("ignore")]
        public bool Ignore = false;

        // zero bounds are not allowed - just
        // set the bound to null if you want
        // to disable it
        private float? _upperBound;
        [ViewVariables]
        [DataField("upperBound")]
        public float? UpperBound
        {
            get => _upperBound;
            set
            {
                if (value == null)
                {
                    _upperBound = null;
                    return;
                }

                if (value <= 0f
                    || value == float.PositiveInfinity
                    || value == float.NaN)
                    return;

                if (LowerBound != null
                    && value < LowerBound)
                    return;

                _upperBound = value;
            }
        }

        private float? _lowerBound;
        [ViewVariables]
        [DataField("lowerBound")]
        public float? LowerBound
        {
            get => _lowerBound;
            set
            {
                if (value == null)
                {
                    _lowerBound = null;
                    return;
                }

                if (value <= 0f
                    || value == float.NaN
                    || value == float.NegativeInfinity)
                    return;

                if (UpperBound != null
                    && value > UpperBound)
                    return;

                _lowerBound = value;
            }
        }

        // upper warning percentage
        // must always cause UpperWarningBound
        // to be smaller
        private float? _upperWarningPercentage;
        [ViewVariables]
        [DataField("upperWarnAround")]
        public float? UpperWarningPercentage
        {
            get => _upperWarningPercentage;
            set
            {
                if (value == null)
                {
                    _upperWarningPercentage = null;
                    return;
                }

                var testValue = value * UpperBound;
                if (value > 1f
                    || testValue < LowerWarningBound
                    || testValue < LowerBound)
                    return;

                _upperWarningPercentage = value;
            }

        }

        // lower warning percentage
        // must always cause LowerWarningBound
        // to be larger
        private float? _lowerWarningPercentage;
        [ViewVariables]
        [DataField("lowerWarnAround")]
        public float? LowerWarningPercentage
        {
            get => _lowerWarningPercentage;
            set
            {
                if (value == null)
                {
                    _lowerWarningPercentage = null;
                    return;
                }

                var testValue = value * LowerBound;
                if (value < 1f
                    || testValue > UpperWarningBound
                    || testValue > UpperBound)
                    return;

                _lowerWarningPercentage = value;
            }
        }

        [ViewVariables]
        public float? UpperWarningBound {
            get
            {
                if (UpperBound == null || UpperWarningPercentage == null) return null;

                return UpperBound * UpperWarningPercentage;
            }
            set
            {
                if (value == null)
                {
                    UpperWarningPercentage = null;
                    return;
                }

                if (UpperBound != null)
                {
                    float? percentage = value / UpperBound;
                    if (percentage != null)
                        UpperWarningPercentage = percentage;
                }
            }
        }

        [ViewVariables]
        public float? LowerWarningBound
        {
            get
            {
                if (LowerBound == null || LowerWarningPercentage == null) return null;

                return LowerBound * LowerWarningPercentage;
            }
            set
            {
                if (value == null)
                {
                    LowerWarningPercentage = null;
                    return;
                }

                if (LowerBound != null)
                {
                    float? percentage = value / LowerBound;
                    if (percentage != null)
                        LowerWarningPercentage = percentage;
                }
            }
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
            else if (value >= UpperWarningBound || value <= LowerWarningBound)
            {
                state = AtmosMonitorAlarmType.Warning;
                return true;
            }

            return false;
        }
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
