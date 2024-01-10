using Content.Shared.StationRecords;

namespace Content.Server.StationRecords.Systems;

/// <summary>
/// Currently just a helper for all records consoles, but could handle some parts of ui in the future.
/// </summary>
public sealed class RecordsConsoleSystem : EntitySystem
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
        var empty = filter == null || filter.Value.Length == 0;
        if (empty)
            return false;

        var filterLowerCaseValue = filter.Value.ToLower();

        return filter.Type switch
        {
            StationRecordFilterType.Name =>
                !someRecord.Name.ToLower().Contains(filterLowerCaseValue),
            StationRecordFilterType.Prints => someRecord.Fingerprint != null
                && IsFilterWithSomeCodeValue(someRecord.Fingerprint, filterLowerCaseValue),
            StationRecordFilterType.DNA => someRecord.DNA != null
                && IsFilterWithSomeCodeValue(someRecord.DNA, filterLowerCaseValue),
        };
    }

    private bool IsFilterWithSomeCodeValue(string value, string filter)
    {
        return !value.ToLower().StartsWith(filter);
    }
}
