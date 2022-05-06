using System.Linq;
using Content.Shared.Voting;
using JetBrains.Annotations;
using Robust.Shared.Random;

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

    private string? _selectedFailsafeMap = null;

    private Dictionary<string, uint> _theoreticalMaxPop = new Dictionary<string, uint>();

    private void InitializeMapSelection()
    {
    }

    private void CalculateTheoreticalMaxPop()
    {
        var canLoadAlongside = _prototypeManager.EnumeratePrototypes<GameMapPrototype>()
            .Where(x => x.MaximumPartners > 1)
            .OrderBy(x => x.MaxPlayers)
            .ToList();
        foreach (var proto in _prototypeManager.EnumeratePrototypes<GameMapPrototype>())
        {
            if (proto.MaximumPartners <= 1)
            {
                _theoreticalMaxPop[proto.ID] = proto.MaxPlayers;
                continue;
            }


        }
    }

    /// <summary>
    /// Updates what maps to use next.
    /// </summary>
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
            {
                _selectedFailsafeMap = null;
                return; // Some other system handled selecting maps for us.
            }
        }

        if (ForcedMaps is not null)
        {
            _nextMaps.Clear();
            _nextMaps.AddRange(ForcedMaps);
            _selectedFailsafeMap = null;
            return;
        }

        if (VotedMap is not null)
        {
            _nextMaps.Clear();
            _nextMaps.Add(VotedMap);
            _selectedFailsafeMap = null;
            return;
        }

        if (_mapRotationEnabled)
        {
            var prioritizedMaps =
                _prototypeManager.EnumeratePrototypes<GameMapPrototype>()
                    .Where(CheckMapIsEligible)
                    .Select(x => (x, GetMapQueuePriority(x.ID)))
                    .OrderByDescending(x => x.Item2);


        }

        // Welp, still no map! Pick from the eligible list randomly, and if that's not an option, pick a fallback.
        _nextMaps.Clear();

        if (_selectedFailsafeMap != null)
        {
            _nextMaps.Add(_selectedFailsafeMap);
            return;
        }

        var eligible = GetEligibleMaps().ToArray();

        if (eligible.Length == 0)
        {
            var fallbacks = _prototypeManager.EnumeratePrototypes<GameMapPrototype>()
                .Where(x => x.Fallback)
                .Select(x => x.ID);
            _nextMaps.Add(_random.Pick(fallbacks.ToArray()));
        }
        else
        {
            _nextMaps.Add(_random.Pick(eligible.Select(x => x.ID).ToArray()));
        }
    }

    private int GetMapQueuePriority(string gameMapProtoName)
    {
        var i = 0;
        var copy = _previousMaps.ToList();
        copy.Reverse(); // can't figure out how to use the LINQ method, which is equivalent to this, here. yay ugly.
        foreach (var map in copy)
        {
            if (map == gameMapProtoName)
                return i;
            i++;
        }

        return _mapRotationMemoryDepth;
    }

    public bool CheckMapIsEligible(GameMapPrototype proto)
    {
        if (_playerManager.PlayerCount > _theoreticalMaxPop[proto.ID] || _playerManager.PlayerCount < proto.MinPlayers)
            return false;

        foreach (var condition in proto.Conditions)
        {
            if (!condition.Check(proto))
                return false;
        }

        return true;
    }

    public IEnumerable<GameMapPrototype> GetEligibleMaps()
    {
        foreach (var map in _prototypeManager.EnumeratePrototypes<GameMapPrototype>())
        {
            if (CheckMapIsEligible(map))
                yield return map;
        }
    }

    public IEnumerable<GameMapPrototype>? TryScaleMapToMeetPop(GameMapPrototype proto)
    {
        if (proto.MaximumPartners <= 1 || proto.MedianPlayers >= _playerManager.PlayerCount)
            return new List<GameMapPrototype> { proto };

        var scalableMaps = _prototypeManager.EnumeratePrototypes<GameMapPrototype>()
            .Where(x => x.MaximumPartners > 1).ToHashSet();

        var selectedMaps = new List<GameMapPrototype>(4) { proto };
        var currentMedPop = proto.MedianPlayers;

        while (true)
        {
            if (scalableMaps.Count == 0)
                return selectedMaps; // If we got here somehow, I surrender, please don't hurt me.

            var mapsShuffled = scalableMaps.ToArray();
            _random.Shuffle(mapsShuffled);
            foreach (var map in mapsShuffled)
            {
                selectedMaps.Add(map);
                if (!map.CanDuplicate)
                    scalableMaps.Remove(map);

                currentMedPop += map.MedianPlayers;

                if (currentMedPop > _playerManager.PlayerCount)
                {
                    return selectedMaps;
                }
            }
        }
    }

    public void ForceMaps(List<string> maps)
    {
        _forcedMaps = maps;
    }

    public void ClearAllOverrides()
    {
        _forcedMaps = null;
        VotedMap = null;
    }
}

[PublicAPI]
public sealed class SelectingMapEvent : HandledEntityEventArgs
{
    public readonly List<string> SelectedMaps = new ();
}
