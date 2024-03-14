using Content.Shared.FixedPoint;

namespace Content.Shared.Medical.Common;

public static class SeverityHelper
{
    private static string[] _physicalStrings = new[]
    {
        "Pristine",
        "Slight Damage",
        "Minor Damage",
        "Damaged",
        "Bad Damage",
        "Severe Damage",
        "Critical Damage"
    };

    private static string[] _severityStrings = new[]
    {
        "Trivial",
        "Minor",
        "Moderate",
        "Major",
        "Extreme",
        "Critical"
    };

    private static string[] _visibleConditionStrings = new[]
    {
        "Pristine",
        "Healthy",
        "Looks OK",
        "Not Great",
        "Looking Bad",
        "Critical"
    };

    public static string GetSeverityString(FixedPoint2 severityPercentage)
    {
        severityPercentage = FixedPoint2.Clamp(severityPercentage, 0, 100)/100;
        FixedPoint2 percentage = 1 / (float)(_severityStrings.Length-1);
        return _severityStrings[(severityPercentage / percentage).Int()];
    }

    public static string GetPhysicalString(FixedPoint2 physicalPercentage)
    {
        physicalPercentage = FixedPoint2.Clamp(physicalPercentage, 0, 1);
        FixedPoint2 percentage = 1 / (float)(_severityStrings.Length-1);
        return _severityStrings[(physicalPercentage / percentage).Int()];
    }

    public static string GetVisibleConditionString(FixedPoint2 severityPercentage)
    {
        severityPercentage = FixedPoint2.Clamp(severityPercentage, 0, 1);
        FixedPoint2 percentage = 1 / (float)(_severityStrings.Length-1);
        return _severityStrings[(severityPercentage / percentage).Int()];
    }
}
