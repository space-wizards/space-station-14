using Content.Shared.FixedPoint;

namespace Content.Shared.Medical.Common;

public static class SeverityHelper
{
    private static string[] _physicalStrings = new[]
    {
        "Critical Damage",
        "Severe Damage",
        "Bad Damage",
        "Damaged",
        "Minor Damage",
        "Slight Damage",
        "Pristine"
    };

    private static string[] _severityStrings = new[]
    {
        "Critical",
        "Extreme",
        "Major",
        "Moderate",
        "Minor",
        "Trivial"
    };

    private static string[] _visibleConditionStrings = new[]
    {
        "Critical",
        "Looking Bad",
        "Not Great",
        "Looks OK",
        "Healthy",
        "Pristine"
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
