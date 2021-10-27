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

        private float? _upperBound;
        [ViewVariables]
        [DataField("upperBound")]
        public float? UpperBound
        {
            get => _upperBound;
            set
            {
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
                if (value > 1f || value * UpperBound < LowerWarningBound) return;

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
                if (value < 1f || value * LowerBound > UpperWarningBound) return;

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
                if (UpperBound != null)
                {
                    float? percentage = value / UpperBound;
                    // ensure that this bound is higher than the lower warning bound
                    if (percentage != null && (UpperBound * percentage) > LowerWarningBound)
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
                if (LowerBound != null)
                {
                    float? percentage = value / LowerBound;
                    // ensure that this bound is lower than the upper warning bound
                    if (percentage != null && (LowerBound * percentage) < UpperWarningBound)
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

    public enum AtmosMonitorThresholdType
    {
        Temperature,
        Pressure,
        Gas
    }
}
