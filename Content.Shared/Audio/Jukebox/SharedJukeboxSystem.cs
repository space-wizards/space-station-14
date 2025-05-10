using Robust.Shared.Audio.Systems;
using Content.Shared.Emag.Systems; //imp

namespace Content.Shared.Audio.Jukebox;

public abstract class SharedJukeboxSystem : EntitySystem
{
    [Dependency] protected readonly SharedAudioSystem Audio = default!;

    //imp for everything below this line ------------------
    //support emagging the jukebox
    [Dependency] protected readonly EmagSystem _emag = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<JukeboxComponent, GotEmaggedEvent>(OnEmagged);
    }

    protected void OnEmagged(EntityUid uid, JukeboxComponent component, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        if (_emag.CheckFlag(uid, EmagType.Interaction))
            return;

        args.Handled = true;
    }
}
