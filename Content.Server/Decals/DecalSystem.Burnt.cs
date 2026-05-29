using System.Linq;
using Content.Shared.Decals;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Decals;

public sealed partial class DecalSystem
{
    [Dependency] private IRobustRandom _robustRandom = default!;

    private string[] _burntDecals = [];

    /// <summary>
    /// Maximum number of burnt decals allowed on a single tile.
    /// </summary>
    public const int MaxBurntDecalsPerTile = 4;

    private void OnDecalPrototypesReloaded(PrototypesReloadedEventArgs ev)
    {
        if (ev.WasModified<DecalPrototype>())
            CacheBurntDecals();
    }

    private void CacheBurntDecals()
    {
        _burntDecals = [.. PrototypeManager.EnumeratePrototypes<DecalPrototype>().Where(p => p.Tags.Contains("burnt")).Select(p => p.ID)];
    }

    /// <summary>
    /// Attempts to place a single random burnt decal on a grid tile.
    /// No decal is placed if the tile already has <paramref name="maxDecals"/> or more burnt decals.
    /// </summary>
    public bool TryAddBurntDecal(EntityUid gridId, Vector2i tilePos, int maxDecals = MaxBurntDecalsPerTile)
    {
        if (_burntDecals.Length == 0)
            return false;

        var decals = GetDecalsInRange(gridId, tilePos);
        var burntCount = 0;
        foreach (var (_, decal) in decals)
        {
            if (Array.IndexOf(_burntDecals, decal.Id) == -1)
                continue;

            burntCount++;
            if (burntCount >= maxDecals)
                return false;
        }

        return TryAddDecal(_burntDecals[_robustRandom.Next(_burntDecals.Length)], new EntityCoordinates(gridId, tilePos), out _, cleanable: true);
    }
}
