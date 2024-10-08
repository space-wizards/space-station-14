using Content.Shared.Actions;
using Content.Shared.Storage;
using System.ComponentModel.DataAnnotations;

namespace Content.Shared.Magic.Events;

public sealed partial class RandomGlobalSpawnSpellEvent : InstantActionEvent, ISpeakSpell
{
    /// <summary>
    /// The list of prototypes this spell can spawn, will select one randomly
    /// </summary>
    [DataField]
    public List<EntitySpawnEntry> Spawns = new();

    /// <summary>
    /// Whether or not to include players without minds (i.e. disconnected from the server).
    /// </summary>
    [DataField]
    public bool RequireMind = true;

    [DataField]
    public string? Speech { get; private set; }
}
