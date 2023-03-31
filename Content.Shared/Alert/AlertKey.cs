using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;

namespace Content.Shared.Alert;

/// <summary>
/// Key for an alert which is unique (for equality and hashcode purposes) w.r.t category semantics.
/// I.e., entirely defined by the category, if a category was specified, otherwise
/// falls back to the id.
/// </summary>
[Serializable, NetSerializable]
public struct AlertKey
{
    public AlertType? AlertType { get; private set; } = Alert.AlertType.Error;
    public readonly AlertCategory? AlertCategory;

    /// NOTE: if the alert has a category you must pass the category for this to work
    /// properly as a key. I.e. if the alert has a category and you pass only the alert type, and you
    /// compare this to another AlertKey that has both the category and the same alert type, it will not consider them equal.
    public AlertKey(AlertType? alertType, AlertCategory? alertCategory)
    {
        AlertCategory = alertCategory;
        AlertType = alertType;
    }

    public bool Equals(AlertKey other)
    {
        // compare only on alert category if we have one
        if (AlertCategory.HasValue)
        {
            return other.AlertCategory == AlertCategory;
        }

        return AlertType == other.AlertType && AlertCategory == other.AlertCategory;
    }

    public override bool Equals(object? obj)
    {
        return obj is AlertKey other && Equals(other);
    }

    public override int GetHashCode()
    {
        // use only alert category if we have one
        if (AlertCategory.HasValue) return AlertCategory.GetHashCode();
        return AlertType.GetHashCode();
    }

    /// <param name="category">alert category, must not be null</param>
    /// <returns>An alert key for the provided alert category. This must only be used for
    /// queries and never storage, as it is lacking an alert type.</returns>
    public static AlertKey ForCategory(AlertCategory category)
    {
        return new(null, category);
    }
}
