using Content.Shared.StationRecords.Components;

namespace Content.Shared.StationRecords.Systems;

public sealed partial class StationRecordsSystem
{
    /// <summary>
    /// Checks if a record should be skipped given a filter.
    /// Takes general record since even if you are using this for e.g. criminal records,
    /// you don't want to duplicate basic info like name and dna.
    /// Station records lets you do this nicely with multiple types having their own data.
    /// </summary>
    public bool IsSkipped(StationRecordsFilter? filter, GeneralStationRecord someRecord)
    {
        // if nothing is being filtered, show everything
        if (filter == null)
            return false;
        if (filter.Value.Length == 0)
            return false;

        var filterLowerCaseValue = filter.Value.ToLower();

        return filter.Type switch
        {
            StationRecordFilterType.Name =>
                !someRecord.Name.Contains(filterLowerCaseValue, StringComparison.OrdinalIgnoreCase),
            StationRecordFilterType.Job =>
                !someRecord.JobTitle.Contains(filterLowerCaseValue, StringComparison.OrdinalIgnoreCase),
            StationRecordFilterType.Species =>
                !someRecord.Species.Contains(filterLowerCaseValue, StringComparison.OrdinalIgnoreCase),
            StationRecordFilterType.Prints => someRecord.Fingerprint != null
                && IsFilterWithSomeCodeValue(someRecord.Fingerprint, filterLowerCaseValue),
            StationRecordFilterType.DNA => someRecord.DNA != null
                && IsFilterWithSomeCodeValue(someRecord.DNA, filterLowerCaseValue),
            _ => throw new IndexOutOfRangeException(nameof(filter.Type)),
        };
    }

    private bool IsFilterWithSomeCodeValue(string value, string filter)
    {
        return !value.StartsWith(filter, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Build a record listing of id to name for a station and filter.
    /// </summary>
    public Dictionary<uint, string> BuildListing(Entity<StationRecordsComponent> station, StationRecordsFilter? filter)
    {
        var listing = new Dictionary<uint, string>();

        var records = GetRecordsOfType<GeneralStationRecord>(station.AsNullable());
        foreach (var pair in records)
        {
            if (IsSkipped(filter, pair.Item2))
                continue;

            listing.Add(pair.Item1, pair.Item2.Name);
        }

        return listing;
    }
}
