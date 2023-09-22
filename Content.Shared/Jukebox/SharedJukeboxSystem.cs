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

        SubscribeLocalEvent<JukeboxComponent, ComponentGetState>(OnJukeboxGetState);
        SubscribeLocalEvent<JukeboxComponent, ComponentHandleState>(OnJukeboxHandleState);
    }

    protected virtual void OnComponentInit(EntityUid uid, JukeboxComponent component, ComponentInit args)
    {
        component.JukeboxMusicCollection = _prototypeManager.Index<MusicListPrototype>(component.MusicCollection);
    }
    public void PlaySong()
    {

    }
    public List<MusicListDefinition> GetList(EntityUid uid, JukeboxComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return new();

        var inventory = new List<MusicListDefinition>(component.JukeboxMusicCollection.Songs);
        return inventory;
    }

    private static void OnJukeboxGetState(EntityUid uid, JukeboxComponent component, ref ComponentGetState args)
    {
        args.State = new JukeboxComponentState(component.Playing, component.SelectedSongID, component.SongTime, component.SongStartTime, component.MusicCollection);
    }

    private void OnJukeboxHandleState(EntityUid uid, JukeboxComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not JukeboxComponentState state)
            return;

        component.Playing = state.Playing;
        component.SelectedSongID = state.SelectedSongID;
        component.SongTime = state.SongTime;
        component.SongStartTime = state.SongStartTime;
        if (state.MusicCollection != null)
            component.MusicCollection = state.MusicCollection;
    }
}
