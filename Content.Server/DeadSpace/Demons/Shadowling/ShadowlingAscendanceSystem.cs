// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DoAfter;
using Content.Shared.DeadSpace.Demons.Shadowling;
using Content.Server.Mind;
using Content.Server.Chat.Systems;
using Content.Shared.Emoting;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Audio;
using Content.Shared.Stunnable;
using Content.Server.Fluids.EntitySystems;
using Content.Shared.Chemistry.Components;
using Robust.Shared.Timing;
using Content.Server.Audio;
using Content.Shared.Audio;
using Robust.Shared.Map;

namespace Content.Server.DeadSpace.Demons.Shadowling;

public sealed class ShadowlingAscendanceSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SmokeSystem _smoke = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ServerGlobalSoundSystem _sound = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ShadowlingAscendanceComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<ShadowlingAscendanceComponent, ShadowlingAscendanceEvent>(OnAscendanceAction);
        SubscribeLocalEvent<ShadowlingAscendanceComponent, ShadowlingAscendanceDoAfterEvent>(OnDoAfter);
    }

    private void OnComponentInit(EntityUid uid, ShadowlingAscendanceComponent component, ComponentInit args)
    {
    }

    private void OnAscendanceAction(EntityUid uid, ShadowlingAscendanceComponent component, ShadowlingAscendanceEvent args)
    {
        if (args.Handled) return;

        var xform = Transform(uid);

        if (xform.GridUid != null)
        {
            var smoke = Spawn("Smoke", new EntityCoordinates(xform.GridUid.Value, xform.Coordinates.Position));
            if (TryComp<SmokeComponent>(smoke, out var smokeComp))
                _smoke.StartSmoke(smoke, new Solution(), 10f, 20, smokeComp);
        }

        _audio.PlayPvs(new SoundCollectionSpecifier("ShadowlingAscendance1"), uid);
        _stun.TryUpdateParalyzeDuration(uid, TimeSpan.FromSeconds(7));

        var doAfterArgs = new DoAfterArgs(EntityManager, uid, component.Duration, new ShadowlingAscendanceDoAfterEvent(), uid)
        {
            BreakOnMove = false,
            BreakOnDamage = false,
            NeedHand = false,
            RequireCanInteract = false,
        };
        _doAfter.TryStartDoAfter(doAfterArgs);

        args.Handled = true;
    }

    private void OnDoAfter(EntityUid uid, ShadowlingAscendanceComponent component, ShadowlingAscendanceDoAfterEvent args)
    {
        if (args.Cancelled) return;

        var xform = Transform(uid);
        var newMob = Spawn("MobShadowlingAscended",
            _transform.GetMapCoordinates(uid, xform),
            rotation: _transform.GetWorldRotation(xform));
        EnsureComp<EmotingComponent>(newMob);

        var query = EntityQueryEnumerator<ShadowlingSlaveComponent>();
        while (query.MoveNext(out var sUid, out var slave))
        {
            if (slave.Master == uid)
            {
                slave.Master = newMob;
                Dirty(sUid, slave);
            }
        }

        if (TryComp<ShadowlingRecruitComponent>(uid, out var oldRecruit) &&
            TryComp<ShadowlingRecruitComponent>(newMob, out var newRecruit))
        {
            newRecruit.TotalRecruited = oldRecruit.TotalRecruited;
            newRecruit.CurrentSlaves = oldRecruit.CurrentSlaves;
        }

        if (_mind.TryGetMind(uid, out var mindId, out var mind))
            _mind.TransferTo(mindId, newMob, mind: mind);

        var message = Loc.GetString("shadowling-ascendance-announcement");
        var sender = Loc.GetString("shadowling-ascendance-sender");

        _audio.PlayPvs(new SoundCollectionSpecifier("ShadowlingAscendance2"), newMob);
        _sound.StopStationEventMusic(newMob, StationEventMusicType.Convergence);
        _sound.DispatchStationEventMusic(newMob, new SoundCollectionSpecifier("ShadowlingAscendance"), StationEventMusicType.Convergence);

        var alreadyAnnounced = false;
        var ruleQuery = EntityQueryEnumerator<ShadowlingRuleComponent>();
        while (ruleQuery.MoveNext(out var ruleComp))
        {
            if (ruleComp.AscendanceAnnounced)
            {
                alreadyAnnounced = true;
                break;
            }
        }

        if (!alreadyAnnounced)
        {
            var allRules = EntityQuery<ShadowlingRuleComponent>();
            foreach (var ruleComp in allRules)
                ruleComp.AscendanceAnnounced = true;

            Timer.Spawn(TimeSpan.FromSeconds(1.48), () =>
            {
                _chat.DispatchGlobalAnnouncement(message, sender,
                    colorOverride: Color.FromHex("#ff0000"),
                    announcementSound: new SoundCollectionSpecifier("ShadowlingAscendanceAnnouncement"));
            });
        }

        var allRulesForAscended = EntityQuery<ShadowlingRuleComponent>();
        foreach (var ruleComp in allRulesForAscended)
            ruleComp.IsAscended = true;

        QueueDel(uid);
    }
}
