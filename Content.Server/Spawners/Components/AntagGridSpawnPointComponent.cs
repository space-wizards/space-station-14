using Content.Server.GameTicking.Rules;
using Content.Shared.Antag;
using Robust.Shared.Prototypes;

namespace Content.Server.Spawners.Components;

/// <summary>
/// Defines a list of antag prototypes which can spawn at a given spawn point with <see cref="RuleGridsSystem"/>
/// There's probably a better way of doing this but there's only so much I can refactor before I drown in soap.
/// </summary>
[RegisterComponent]
public sealed partial class AntagGridSpawnPointComponent : Component
{
    /// <summary>
    /// Whitelist of entities that can be spawned at this SpawnPoint
    /// </summary>
    [DataField]
    public HashSet<ProtoId<AntagSpecifierPrototype>> Whitelist;
}
