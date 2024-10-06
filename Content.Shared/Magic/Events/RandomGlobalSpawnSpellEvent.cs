using Content.Shared.Actions;
using Content.Shared.Storage;
using System.ComponentModel.DataAnnotations;

namespace Content.Shared.Magic.Events;

public sealed partial class RandomGlobalSpawnSpellEvent : InstantActionEvent, ISpeakSpell
{
    [DataField(required: true)]
    public List<EntitySpawnEntry> Spawns;

    [DataField]
    public string? Speech { get; private set; }
}
