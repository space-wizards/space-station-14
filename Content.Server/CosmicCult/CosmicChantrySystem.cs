using Content.Server.Antag;
using Content.Server.Audio;
using Content.Server.Chat.Systems;
using Content.Server.Pinpointer;
using Content.Server.Popups;
using Content.Shared.CosmicCult;
using Content.Shared.CosmicCult.Components;
using Content.Shared.DoAfter;
using Content.Shared.Mind;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.CosmicCult;
public sealed partial class CosmicChantrySystem : EntitySystem
{
    [Dependency] private AntagSelectionSystem _antag = default!;
    [Dependency] private ChatSystem _chatSystem = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private ServerGlobalSoundSystem _sound = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedRoleSystem _role = default!;
    [Dependency] private NavMapSystem _navMap = default!;

    /// <summary>
    /// Mind role to add to colossi.
    /// </summary>
    public static readonly EntProtoId MindRole = "MindRoleCosmicColossus";

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var chantryQuery = EntityQueryEnumerator<CosmicChantryComponent>();
        while (chantryQuery.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime >= comp.SpawnTimer && !comp.Spawned)
            {
                _appearance.SetData(uid, ChantryVisuals.Status, ChantryStatus.On);
                _popup.PopupCoordinates(Loc.GetString("cosmiccult-chantry-powerup"), Transform(uid).Coordinates, PopupType.LargeCaution);
                comp.Spawned = true;

                var doAfterArgs = new DoAfterArgs(EntityManager, uid, comp.EventTime, new CosmicChantryDoAfter(), uid, comp.InternalVictim)
                {
                    NeedHand = false,
                    BreakOnWeightlessMove = false,
                    BreakOnMove = false,
                    BreakOnHandChange = false,
                    BreakOnDropItem = false,
                    BreakOnDamage = false,
                    RequireCanInteract = false,
                };
                _doAfter.TryStartDoAfter(doAfterArgs);
            }
        }
    }

    [SubscribeLocalEvent]
    private void OnDoAfter(Entity<CosmicChantryComponent> ent, ref CosmicChantryDoAfter args)
    {
        if (!_mind.TryGetMind(ent.Comp.InternalVictim, out var mindEnt, out var mind))
            return;
        mind.PreventGhosting = false;
        var tgtpos = Transform(ent).Coordinates;
        var colossus = Spawn(ent.Comp.Colossus, tgtpos);
        _mind.TransferTo(mindEnt, colossus);
        _mind.TryAddObjective(mindEnt, mind, "CosmicFinalityObjective");
        _role.MindAddRole(mindEnt, MindRole, mind, true);
        _antag.SendBriefing(colossus, Loc.GetString("cosmiccult-silicon-colossus-briefing"), Color.FromHex("#4cabb3"), null);
        Spawn(ent.Comp.SpawnVfx, tgtpos);
        QueueDel(ent.Comp.InternalVictim);
        QueueDel(ent);
    }

    [SubscribeLocalEvent]
    private void OnChantryStarted(Entity<CosmicChantryComponent> ent, ref ComponentInit args)
    {
        var indicatedLocation = FormattedMessage.RemoveMarkupOrThrow(_navMap.GetNearestBeaconString((ent, Transform(ent))));
        var comp = ent.Comp;

        comp.SpawnTimer = _timing.CurTime + comp.SpawningTime;
        comp.CountdownTimer = _timing.CurTime + comp.EventTime;

        _sound.PlayGlobalOnStation(ent, _audio.ResolveSound(comp.ChantryAlarm));
        _chatSystem.DispatchStationAnnouncement(ent,
        Loc.GetString("cosmiccult-chantry-location", ("location", indicatedLocation)),
        null, false, null,
        Color.FromHex("#cae8e8"));

        if (_mind.TryGetMind(comp.InternalVictim, out _, out var mind))
            mind.PreventGhosting = true;
    }

    [SubscribeLocalEvent]
    private void OnChantryDestroyed(Entity<CosmicChantryComponent> ent, ref ComponentShutdown args)
    {
        var comp = ent.Comp;
        if (!_mind.TryGetMind(comp.InternalVictim, out var mindId, out var mind))
            return;
        if (TerminatingOrDeleted(comp.VictimBody))
        {
            var tgtpos = Transform(comp.InternalVictim).Coordinates;
            var fallbackEnt = Spawn(comp.FallbackBrain, tgtpos);
            Spawn(comp.FallbackVfx, tgtpos);
            mind.PreventGhosting = false;
            _mind.TransferTo(mindId, fallbackEnt);
            QueueDel(comp.InternalVictim);
        }
        else
        {
            mind.PreventGhosting = false;
            _mind.TransferTo(mindId, comp.VictimBody);
            QueueDel(comp.InternalVictim);
        }
    }
}
