using System.Linq;
using Robust.Shared.Prototypes;

namespace Content.Shared.CF.Character;

/// <summary>
/// A Pathfinder heritage. An empty <see cref="Ancestries"/> list is a versatile heritage and can be taken by anyone.
/// </summary>
[Prototype("cfHeritage")]
public sealed partial class CFHeritagePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name;

    [DataField(required: true)]
    public LocId Description;

    [DataField]
    public CFRarity Rarity = CFRarity.Common;

    /// <summary>Ancestries that can take this heritage. Empty means any ancestry.</summary>
    [DataField]
    public List<ProtoId<CFAncestryPrototype>> Ancestries = new();

    /// <summary>Replaces the ancestry Hit Points when greater than 0.</summary>
    [DataField]
    public int HitPoints;

    /// <summary>Replaces the ancestry Speed in feet when greater than 0.</summary>
    [DataField]
    public int Speed;

    /// <summary>Replaces the ancestry size when set.</summary>
    [DataField]
    public CFSize? Size;

    /// <summary>Grants darkvision outright.</summary>
    [DataField]
    public bool Darkvision;

    /// <summary>
    /// Grants darkvision when the ancestry already has low-light vision or darkvision.
    /// Otherwise the heritage only grants low-light vision, which is listed and not a separate overlay.
    /// </summary>
    [DataField]
    public bool UpgradeVision;

    public bool Fits(string? ancestryId)
    {
        if (Ancestries.Count == 0)
            return true;

        return ancestryId != null && Ancestries.Any(ancestry => ancestry.Id == ancestryId);
    }
}
