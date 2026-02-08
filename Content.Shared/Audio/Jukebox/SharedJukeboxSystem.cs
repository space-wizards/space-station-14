using Robust.Shared.Prototypes;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.Audio.Jukebox;

public abstract class SharedJukeboxSystem : EntitySystem
{
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] protected readonly IPrototypeManager _protoManager = default!;

    public IEnumerable<JukeboxPrototype> GetAvailableTracks(Entity<JukeboxComponent> ent)
    {
        return _protoManager.EnumeratePrototypes<JukeboxPrototype>();
    }
}
