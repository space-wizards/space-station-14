using System.Linq;
using System.Threading.Tasks;
using Content.Server.Popups;
using Content.Shared.DeadSpace.Ports.Jukebox;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using Content.Shared.Popups;
using Robust.Server.Containers;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Server.DeadSpace.Ports.Jukebox;

public sealed class TapeCreatorSystem : EntitySystem
{
    [Dependency] private readonly ServerJukeboxSongsSyncManager _songsSyncManager = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    private const string TapeCreatorContainerName = "tape_creator_container";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<JukeboxSongUploadRequest>(OnSongUploaded);
        SubscribeLocalEvent<TapeCreatorComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<TapeCreatorComponent, InteractUsingEvent>(OnInteract);
        SubscribeLocalEvent<TapeCreatorComponent, GetVerbsEvent<Verb>>(OnTapeCreatorGetVerb);
        SubscribeLocalEvent<TapeCreatorComponent, ComponentGetState>(OnTapeCreatorStateChanged);
        SubscribeLocalEvent<TapeComponent, ComponentGetState>(OnTapeStateChanged);
    }

    private void OnTapeCreatorGetVerb(EntityUid uid, TapeCreatorComponent component, GetVerbsEvent<Verb> ev)
    {
        if (component.Recording) return;
        if (ev.Hands == null) return;
        if (component.TapeContainer.ContainedEntities.Count == 0) return;

        var removeTapeVerb = new Verb
        {
            Text = "Вытащить касету",
            Priority = 10000,
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/remove_tape.png")),
            Act = () =>
            {
                var tapes = component.TapeContainer.ContainedEntities.ToList();
                _container.EmptyContainer(component.TapeContainer, true);

                foreach (var tape in tapes)
                {
                    _hands.PickupOrDrop(ev.User, tape);
                }

                component.InsertedTape = null;
                Dirty(uid, component);
            }
        };

        ev.Verbs.Add(removeTapeVerb);
    }

    private void OnTapeStateChanged(EntityUid uid, TapeComponent component, ref ComponentGetState args)
    {
        args.State = new TapeComponentState
        {
            Songs = component.Songs
        };
    }

    private void OnTapeCreatorStateChanged(EntityUid uid, TapeCreatorComponent component, ref ComponentGetState args)
    {
        args.State = new TapeCreatorComponentState
        {
            Recording = component.Recording,
            CoinBalance = component.CoinBalance,
            InsertedTape = component.InsertedTape
        };
    }

    private void OnComponentInit(EntityUid uid, TapeCreatorComponent component, ComponentInit args)
    {
        component.TapeContainer = _container.EnsureContainer<Container>(uid, TapeCreatorContainerName);
    }

    private void OnInteract(EntityUid uid, TapeCreatorComponent component, InteractUsingEvent args)
    {
        if (component.Recording)
        {
            return;
        }

        if (HasComp<TapeComponent>(args.Used))
        {
            var containedEntities = component.TapeContainer.ContainedEntities;

            if (containedEntities.Count > 1)
            {
                var removedTapes = _container.EmptyContainer(component.TapeContainer, true).ToList();
                _container.Insert(args.Used, component.TapeContainer);

                foreach (var tapes in removedTapes)
                {
                    _hands.PickupOrDrop(args.User, tapes);
                }
            }
            else
            {
                _container.Insert(args.Used, component.TapeContainer);
            }

            component.InsertedTape = GetNetEntity(args.Used);
            Dirty(uid, component);
            return;
        }

        if (_tag.HasTag(args.Used, "TapeRecorderrCoin"))
        {
            Del(args.Used);
            component.CoinBalance += 1;
            Dirty(uid, component);
        }
    }

    private void OnSongUploaded(JukeboxSongUploadRequest ev)
    {
        var tapeCreator = GetEntity(ev.TapeCreatorUid);
        if (!TryComp<TapeCreatorComponent>(tapeCreator, out var tapeCreatorComponent))
        {
            return;
        }

        if (!tapeCreatorComponent.InsertedTape.HasValue || tapeCreatorComponent.CoinBalance <= 0)
        {
            _popup.PopupEntity("Т# %ак@ э*^о сdf{ал б2я~b? Запись была прервана.", tapeCreator);
            return;
        }

        tapeCreatorComponent.CoinBalance -= 1;
        tapeCreatorComponent.Recording = true;

        var insertedTape = GetEntity(tapeCreatorComponent.InsertedTape.Value);
        var tapeComponent = Comp<TapeComponent>(insertedTape);
        var songData = _songsSyncManager.SyncSongData(ev.SongName, ev.SongBytes);

        var song = new JukeboxSong
        {
            SongName = songData.SongName,
            SongPath = songData.Path
        };

        tapeComponent.Songs.Add(song);

        DirtyEntity(GetEntity(ev.TapeCreatorUid));
        Dirty(insertedTape, tapeComponent);

        Record(tapeCreator, tapeCreatorComponent, _popup, _container);
    }

    private void Record(
        EntityUid uid,
        TapeCreatorComponent component,
        SharedPopupSystem popupSystem,
        SharedContainerSystem containerSystem)
    {
        containerSystem.EmptyContainer(component.TapeContainer, force: true);

        component.Recording = false;
        component.InsertedTape = null;

        popupSystem.PopupEntity("Запись мозговой активности завершена", uid);
        Dirty(uid, component);
    }
}
