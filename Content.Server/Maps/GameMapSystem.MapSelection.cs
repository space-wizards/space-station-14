using System.Linq;
using Content.Shared.Voting;
using JetBrains.Annotations;
using Robust.Server.Maps;
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

    public List<(GameMapPrototype proto, MapLoadOptions options, int mapIdx)> GetMapsToLoad()
    {
        throw new NotImplementedException();
    }
}

[PublicAPI]
public sealed class SelectingMapEvent : HandledEntityEventArgs
{
    public readonly List<string> SelectedMaps = new ();
}
