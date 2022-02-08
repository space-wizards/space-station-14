using Content.Server.Dynamic.Prototypes;
using Content.Server.Storage.Components;

namespace Content.Server.Dynamic.Systems;

public partial class DynamicModeSystem
{
    /// <summary>
    ///     The current storyteller.
    /// </summary>
    [ViewVariables]
    public StorytellerPrototype? CurrentStoryteller;

    /// <summary>
    ///     Picks a new storyteller. Called before threat and budgets are dealt with.
    /// </summary>
    public void PickStoryteller()
    {
        // If, for example, an admin forced a storyteller,
        // or a vote from the lobby selected one.
        if (CurrentStoryteller != null)
            return;

        var collection = new WeightedRandomCollection<StorytellerPrototype>(_random);
        foreach (var storyteller in _proto.EnumeratePrototypes<StorytellerPrototype>())
        {
            collection.AddEntry(storyteller, storyteller.Weight);
        }

        CurrentStoryteller = collection.Pick();
    }

    /// <summary>
    ///     Sets the current storyteller to a new one.
    /// </summary>
    /// <remarks>
    ///     Does not rerun threats or events or anything, so this
    ///     should really only be called before roundstart.
    ///
    ///     It will technically affect midround events, though.
    /// </remarks>
    public void ForceSetStoryteller(string storyteller)
    {
        if (!_proto.TryIndex<StorytellerPrototype>(storyteller, out var proto))
            return;

        ForceSetStoryteller(proto);
    }

    public void ForceSetStoryteller(StorytellerPrototype proto)
    {
        CurrentStoryteller = proto;
    }
}
