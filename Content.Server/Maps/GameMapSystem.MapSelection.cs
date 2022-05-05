using System.Linq;
using JetBrains.Annotations;

namespace Content.Server.Maps;

public sealed partial class GameMapSystem
{
    private List<string>? _forcedMaps;

    /// <summary>
    /// The currently forced maps, if any.
    /// </summary>
    public IReadOnlyList<string>? ForcedMaps => _forcedMaps;

    /// <summary>
    /// The map that was voted for, if any.
    /// </summary>
    public string? VotedMap;

    /// <summary>
    /// The maps that the current selection algorithm has chosen.
    /// </summary>
    private readonly List<string> _nextMaps = new();

    public IReadOnlyList<string> NextMaps => _nextMaps;

    private void InitializeMapSelection()
    {

    }

    private void UpdateNextMaps()
    {
        // We don't immediately clear nextmaps in case an exception occurs here. We don't want that list to be empty.
        var ev = new SelectingMapEvent();
        RaiseLocalEvent(ev);
        if (ev.Handled)
        {
            _nextMaps.Clear();
            _nextMaps.AddRange(ev.SelectedMaps);

            if (_nextMaps.Count != 0)
                return; // Some other system handled selecting maps for us.
        }

        if (ForcedMaps is not null)
        {
            _nextMaps.Clear();
            _nextMaps.AddRange(ForcedMaps);
            return;
        }

        if (VotedMap is not null)
        {
            _nextMaps.Clear();
            _nextMaps.Add(VotedMap);
            return;
        }

        if (_mapRotationEnabled)
        {

        }
        // Welp, still no map! Pick from the fallbacks.
        _nextMaps.Clear();
        _nextMaps.AddRange(_prototypeManager.EnumeratePrototypes<GameMapPrototype>().Where(x => x.Fallback).Select(x => x.ID));
    }

    public

    public void ForceMaps(List<string> maps)
    {
        _forcedMaps = maps;
    }

    public void ClearAllOverrides()
    {
        ClearForcedMap();
        ClearVotedMap();
    }

    public void ClearForcedMap()
    {
        _forcedMaps = null;
    }

    public void ClearVotedMap()
    {
        VotedMap = null;
    }
}

[PublicAPI]
public sealed class SelectingMapEvent : HandledEntityEventArgs
{
    public readonly List<string> SelectedMaps = new ();
}
