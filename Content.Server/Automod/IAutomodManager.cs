using System.Threading.Tasks;
using Content.Shared.Automod;
using Content.Shared.Database;
using Robust.Shared.Player;

namespace Content.Server.Automod;

public interface IAutomodManager
{
    public void Initialize();

    /// <summary>
    /// <inheritdoc cref="CreateFilter(AutomodFilterDef)"/>
    /// </summary>
    /// <inheritdoc cref="AutomodFilterDef"/>
    public void CreateFilter(string pattern,
        AutomodFilterType filterType,
        string actionGroup,
        AutomodTarget targets,
        string name);

    /// <summary>
    /// Add a censor def to the manager.
    /// This method sorts the definitions for faster lookup.
    /// </summary>
    /// <param name="automod">The censor to add.</param>
    public void CreateFilter(AutomodFilterDef automod);

    /// <summary>
    /// Checks a message for any matching regex censors.
    /// If there is a match, it runs <see cref="ITextAutomodAction"/>s on the text and matches.
    /// </summary>
    /// <param name="target"></param>
    /// <param name="inputText"></param>
    /// <param name="session"></param>
    /// <returns>True if the message passes. False if the message should be blocked.</returns>
    public bool Filter(AutomodTarget target, string inputText, ICommonSession session);

    /// <summary>
    /// Clears and reloads all censors from the database.
    /// </summary>
    public void ReloadAutomodFilters();

    /// <summary>
    /// Edit an automod filter.
    /// </summary>
    /// <param name="automodFilterDef">The Id determines which filter to replace.</param>
    public void EditFilter(AutomodFilterDef automodFilterDef);

    /// <summary>
    /// Gets an automod filter.
    /// </summary>
    /// <param name="id"></param>
    public Task<AutomodFilterDef?> GetFilter(int id);

    /// <summary>
    /// Remove an automod filter.
    /// </summary>
    /// <param name="id">The id of the filter to delete.</param>
    /// <returns>True when the id is found and deleted.</returns>
    public Task<bool> RemoveFilter(int id);

    /// <summary>
    /// Remove multiple automod filters.
    /// </summary>
    /// <param name="ids">The ids of the filters to delete.</param>
    public Task RemoveMultipleFilters(List<int> ids);
}
