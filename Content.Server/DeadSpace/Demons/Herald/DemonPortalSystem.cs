// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Server.Chat.Systems;
using Content.Server.Pinpointer;
using Robust.Shared.Timing;
using Content.Shared.DeadSpace.Demons.Herald.Components;
using Content.Shared.Destructible;
using Robust.Shared.Player;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Utility;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;

namespace Content.Server.DeadSpace.Demons.Herald;

public sealed class DemonPortalSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly NavMapSystem _navMap = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DemonPortalComponent, DestructionEventArgs>(OnDestr);
        SubscribeLocalEvent<DemonPortalComponent, AnchorStateChangedEvent>(OnAnchorChange);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<DemonPortalComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {

            if (_gameTiming.CurTime > comp.AnnounceTime)
            {
                Announce(uid, comp, xform);
            }

            if (_gameTiming.CurTime > comp.DemonSpawnTime)
            {
                SpawnDemon(comp, xform);
            }
        }
    }

    private void OnDestr(EntityUid uid, DemonPortalComponent component, DestructionEventArgs args)
    {
        var location = Transform(uid).Coordinates;
        Timer.Spawn(20000,
        () => _chat.DispatchGlobalAnnouncement(Loc.GetString("demon-portal-destroyed", ("location", location)), playSound: true, colorOverride: Color.Green));
    }

    private void SpawnDemon(DemonPortalComponent comp, TransformComponent xform)
    {
        var spawn = _random.Pick(comp.DemonSpawnIdArray);
        Spawn(spawn, xform.Coordinates);

        comp.DemonSpawnTime = _gameTiming.CurTime + comp.DemonSpawnDuration;
    }

    private void Announce(EntityUid uid, DemonPortalComponent comp, TransformComponent xform)
    {
        var msg = Loc.GetString("carp-rift-warning",
            ("location", FormattedMessage.RemoveMarkupOrThrow(_navMap.GetNearestBeaconString((uid, xform)))));

        _chat.DispatchGlobalAnnouncement(msg, playSound: false, colorOverride: Color.Red);
        _audio.PlayGlobal("/Audio/Misc/notice1.ogg", Filter.Broadcast(), true);
        _navMap.SetBeaconEnabled(uid, true);
        comp.AnnounceTime = _gameTiming.CurTime + comp.AnnounceDuration;
    }

    private void OnAnchorChange(EntityUid uid, DemonPortalComponent component, ref AnchorStateChangedEvent args)
    {
        if (!args.Anchored)
        {
            QueueDel(uid);
        }
    }
}
