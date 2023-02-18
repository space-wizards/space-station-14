using Content.Shared.Ninja;

namespace Content.Server.Ninja.Components;

[RegisterComponent]
public sealed class SpaceNinjaComponent : Component
{
    /// Currently worn suit
    [ViewVariables]
    public EntityUid? Suit = null;

    /// Currently worn gloves
    [ViewVariables]
    public EntityUid? Gloves = null;

    /// Number of doors that have been doorjacked, used for objective
    [ViewVariables]
    public int DoorsJacked = 0;

    /// Research nodes that have been downloaded, used for objective
    [ViewVariables]
    public HashSet<string> DownloadedNodes = new();

    /// Warp point that the spider charge has to target
    [ViewVariables]
    public EntityUid? SpiderChargeTarget = null;

    /// Whether the spider charge has been detonated on the target, used for objective
    [ViewVariables]
    public bool SpiderChargeDetonated;

    /// Whether the comms console has been hacked, used for objective
    [ViewVariables]
    public bool CalledInThreat;
}
