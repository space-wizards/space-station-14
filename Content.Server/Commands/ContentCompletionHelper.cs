using Content.Shared.Station.Components;
using Robust.Shared.Console;

namespace Content.Server.Commands;

/// <summary>
/// Helper functions for programming console command completions.
/// </summary>
public static class ContentCompletionHelper
{
    /// <summary>
    /// Return all stations, with their ID as value and name as hint.
    /// </summary>
    public static IEnumerable<CompletionOption> StationIds(IEntityManager entityManager)
    {
        var query = entityManager.EntityQueryEnumerator<StationDataComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out _, out var metaData))
        {
            yield return new CompletionOption(uid.ToString(), metaData.EntityName);
        }
    }

    public static IEnumerable<CompletionOption> ByComponentAndEntityUid<T>(string text, IEntityManager entManager, int limit = 20) where T : IComponent
    {
        if (text != string.Empty && !EntityUid.TryParse(text, out _))
            yield break;

        var query = entManager.AllEntityQueryEnumerator<T, MetaDataComponent>();

        var i = 0;
        while (i < limit && query.MoveNext(out var uid, out _, out var metadata))
        {
            var uidText = uid.ToString();
            if (uidText?.StartsWith(text) != true)
                continue;

            i++;
            yield return new CompletionOption(uidText, metadata.EntityName);
        }
    }

}
