using System.Collections.Immutable;
using Content.Server.CosmicCult.Components;
using Content.Server.GameTicking.Rules;
using Content.Stellar.Server.CosmicCult.Components;
using Content.Server.Popups;
using Content.Shared.CosmicCult;
using Content.Shared.CosmicCult.Components;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mindshield.Components;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.CosmicCult.Abilities;

public sealed partial class CosmicShuntSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private CosmicCultRuleSystem _cultRule = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedCosmicCultSystem _cult = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedStunSystem _stun = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var shuntQuery = EntityQueryEnumerator<CosmicShuntedEntityComponent>();
        while (shuntQuery.MoveNext(out var uid, out var comp))
        {
            if (comp.ReadyToReturn || _timing.CurTime >= comp.ExitVoidTime)
            {
                if (!_mind.TryGetMind(uid, out var mindEnt, out var mind) || _cultRule.AssociatedGamerule(comp.ShuntCaster) is not { } cultRule)
                    continue;

                mind.PreventGhosting = false;
                _mind.TransferTo(mindEnt, comp.OriginalBody);
                _popup.PopupEntity(Loc.GetString("cosmicability-shunt-return"), comp.OriginalBody, comp.OriginalBody);

                if (HasComp<MindShieldComponent>(comp.OriginalBody) && cultRule.Comp.Tier != 3)
                {
                    comp.ConvertOnReturn = false;
                    _popup.PopupCoordinates(Loc.GetString("cosmicability-shunt-conversion-fail-body", ("target", Identity.Entity(comp.OriginalBody, EntityManager))), Transform(comp.OriginalBody).Coordinates, PopupType.Large);
                    _popup.PopupCoordinates(Loc.GetString("cosmicability-shunt-conversion-fail-wisp"), Transform(uid).Coordinates, PopupType.LargeCaution);
                }
                // This is where whitelisting would go if you want to prevent other things from being converted.

                if (comp.ConvertOnReturn)
                {
                    _cultRule.CosmicConversion(comp.ShuntCaster, comp.OriginalBody);
                    _stun.SetKnockdownTime(comp.OriginalBody, TimeSpan.Zero);
                    _audio.PlayPvs(comp.WispGrabber.Comp.TriggerSfx, Transform(comp.OriginalBody).Coordinates);
                    _audio.PlayPvs(comp.WispGrabber.Comp.TriggerSfx, Transform(uid).Coordinates);
                    Spawn(comp.WispGrabber.Comp.GenericVfx, Transform(comp.OriginalBody).Coordinates);
                    Spawn(comp.WispGrabber.Comp.GenericVfx, Transform(uid).Coordinates);

                    if (comp.WispGrabber.Owner == comp.ShuntCaster.Owner)
                        _cultRule.IncrementCultistProgress(comp.WispGrabber, 3);
                    else
                    {
                        _cultRule.IncrementCultistProgress(comp.ShuntCaster, 3);
                        _cultRule.IncrementCultistProgress(comp.WispGrabber, 3);
                    }

                    comp.ConvertOnReturn = false;
                }

                RemComp<CosmicShuntedOriginComponent>(comp.OriginalBody);
                RemComp<CosmicExamineComponent>(comp.OriginalBody);
                QueueDel(uid);
            }
        }
    }

    [SubscribeLocalEvent]
    private void OnShuntInteract(Entity<CosmicShuntedEntityComponent> ent, ref InteractHandEvent args)
    {
        if (ent.Comp.ConvertOnReturn || args.Handled || !_cult.EntityIsCultist(args.User) || !TryComp<CosmicCultistComponent>(args.User, out var cultComp))
            return;

        ent.Comp.WispGrabber = (args.User, cultComp);
        ent.Comp.ConvertOnReturn = true;
        ent.Comp.ReadyToReturn = true;
    }

    [SubscribeLocalEvent]
    private void OnCosmicShunt(Entity<CosmicCultistComponent> ent, ref EventCosmicShunt args)
    {
        if (args.Handled)
            return;

        var doargs = new DoAfterArgs(EntityManager, ent, ent.Comp.CosmicShuntDelay, new CosmicShuntDoAfter(), ent, args.Target)
        {
            DistanceThreshold = 1.5f, Hidden = false, BreakOnDamage = true, BreakOnMove = true, BreakOnDropItem = true,
        };
        args.Handled = true;
        _doAfter.TryStartDoAfter(doargs);
        _popup.PopupEntity(Loc.GetString("cosmicability-shunt-begin", ("target", Identity.Entity(ent, EntityManager))), ent, args.Target);
    }

    [SubscribeLocalEvent]
    private void OnCosmicShuntDoAfter(Entity<CosmicCultistComponent> ent, ref CosmicShuntDoAfter args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target is not { } target)
            return;

        if (!TryComp<MindContainerComponent>(target, out var mindContainer) || mindContainer.Mind is not { } mindEnt)
            return;

        var spawnPoints = EntityManager.GetAllComponents(typeof(CosmicVoidSpawnComponent)).ToImmutableList();
        if (spawnPoints.IsEmpty)
            return;

        EnsureComp<CosmicShuntedOriginComponent>(target);
        var examine = EnsureComp<CosmicExamineComponent>(target);
        examine.CultistText = "cosmic-examine-text-abilityshunt";

        _popup.PopupEntity(Loc.GetString("cosmicability-shunt-success", ("target", Identity.Entity(target, EntityManager))), ent, ent);
        var tgtpos = Transform(target).Coordinates;
        var mind = Comp<MindComponent>(mindEnt);
        mind.PreventGhosting = true;

        _audio.PlayPvs(ent.Comp.ShuntSfx, ent, AudioParams.Default.WithVolume(6f));
        Spawn(ent.Comp.ShuntVfx, tgtpos);
        var newSpawn = _random.Pick(spawnPoints);
        var spawnTgt = Transform(newSpawn.Uid).Coordinates;
        var wisp = Spawn(ent.Comp.SpawnWisp, spawnTgt);

        EnsureComp<CosmicShuntedEntityComponent>(wisp, out var shuntComp);
        shuntComp.ShuntCaster = ent;
        shuntComp.OriginalBody = target;
        shuntComp.ExitVoidTime = _timing.CurTime + ent.Comp.CosmicShuntDuration;

        _mind.TransferTo(mindEnt, wisp);
        _stun.TryKnockdown(target, ent.Comp.CosmicShuntDuration + TimeSpan.FromSeconds(2));
        _popup.PopupEntity(Loc.GetString("cosmicability-shunt-transfer"), wisp, wisp);
        _audio.PlayPvs(ent.Comp.ShuntSfx, spawnTgt, AudioParams.Default.WithVolume(6f));
        Spawn(ent.Comp.ShuntVfx, spawnTgt);
        args.Handled = true;
    }
}
