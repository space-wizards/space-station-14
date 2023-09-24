using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Jukebox;

public abstract class SharedJukeboxSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<JukeboxComponent, ComponentInit>(OnComponentInit);
    }

    protected virtual void OnComponentInit(EntityUid uid, JukeboxComponent component, ComponentInit args)
    {
        component.JukeboxMusicCollection = _prototypeManager.Index<MusicListPrototype>(component.MusicCollection);
    }
    public List<MusicListDefinition> GetList(EntityUid uid, JukeboxComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return new();

        var inventory = new List<MusicListDefinition>(component.JukeboxMusicCollection.Songs);
        return inventory;
    }
}
