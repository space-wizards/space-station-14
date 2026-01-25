using Content.Server.Antag;
using Content.Server.Cloning;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Medical.SuitSensors;
using Content.Server.Objectives.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Gibbing.Components;
using Content.Shared.Medical.SuitSensor;
using Content.Shared.Mind;
using Robust.Shared.Random;

namespace Content.Server.GameTicking.Rules;

public sealed class ParadoxCloneRuleSystem : GameRuleSystem<ParadoxCloneRuleComponent>
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly CloningSystem _cloning = default!;
    [Dependency] private readonly SuitSensorSystem _sensor = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ParadoxCloneRuleComponent, AntagSelectEntityEvent>(OnAntagSelectEntity);
        SubscribeLocalEvent<ParadoxCloneRuleComponent, AfterAntagEntitySelectedEvent>(AfterAntagEntitySelected);
    }

    protected override void Started(EntityUid uid, ParadoxCloneRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        // check if we got enough potential cloning targets, otherwise cancel the gamerule so that the ghost role does not show up
        var allHumans = _mind.GetAliveHumans();

        if (allHumans.Count == 0)
        {
            Log.Info("Could not find any alive players to create a paradox clone from! Ending gamerule.");
            ForceEndSelf(uid, gameRule);
        }
    }

    // we have to do the spawning here so we can transfer the mind to the correct entity and can assign the objectives correctly
    private void OnAntagSelectEntity(Entity<ParadoxCloneRuleComponent> ent, ref AntagSelectEntityEvent args)
    {
        if (args.Session?.AttachedEntity is not { } spawner)
            return;

        if (ent.Comp.OriginalBody != null) // target was overridden, for example by admin antag control
        {
            if (Deleted(ent.Comp.OriginalBody.Value) || !_mind.TryGetMind(ent.Comp.OriginalBody.Value, out var originalMindId, out var _))
            {
                Log.Warning("Could not find mind of target player to paradox clone!");
                return;
            }
            ent.Comp.OriginalMind = originalMindId;
        }
        else
        {
            // get possible targets
            var allAliveHumanoids = _mind.GetAliveHumans();

            // we already checked when starting the gamerule, but someone might have died since then.
            if (allAliveHumanoids.Count == 0)
            {
                Log.Warning("Could not find any alive players to create a paradox clone from!");
                return;
            }

            // pick a random player
            var randomHumanoidMind = _random.Pick(allAliveHumanoids);
            ent.Comp.OriginalMind = randomHumanoidMind;
            ent.Comp.OriginalBody = randomHumanoidMind.Comp.OwnedEntity;

        }

        if (ent.Comp.OriginalBody == null || !_cloning.TryCloning(ent.Comp.OriginalBody.Value, _transform.GetMapCoordinates(spawner), ent.Comp.Settings, out var clone))
        {
            Log.Error($"Unable to make a paradox clone of entity {ToPrettyString(ent.Comp.OriginalBody)}");
            return;
        }

        var targetComp = EnsureComp<TargetOverrideComponent>(clone.Value);
        targetComp.Target = ent.Comp.OriginalMind; // set the kill target

        var gibComp = EnsureComp<GibOnRoundEndComponent>(clone.Value);
        gibComp.SpawnProto = ent.Comp.GibProto;
        gibComp.PreventGibbingObjectives = new() { "ParadoxCloneKillObjective" }; // don't gib them if they killed the original.

        // turn their suit sensors off so they don't immediately get noticed
        _sensor.SetAllSensors(clone.Value, SuitSensorMode.SensorOff);

        args.Entity = clone;
    }

    private void AfterAntagEntitySelected(Entity<ParadoxCloneRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        if (ent.Comp.OriginalMind == null)
            return;

        if (!_mind.TryGetMind(args.EntityUid, out var cloneMindId, out var cloneMindComp))
            return;

        _mind.CopyObjectives(ent.Comp.OriginalMind.Value, (cloneMindId, cloneMindComp), ent.Comp.ObjectiveWhitelist, ent.Comp.ObjectiveBlacklist);
    }
}
