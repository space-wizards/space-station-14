using System.Linq;
using Content.Server.Anomaly.Components;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Anomaly.Components;
using Robust.Shared.Random;
using Content.Shared.Chemistry.Reagent;
using Robust.Server.GameObjects;
using Content.Shared.Chemistry.Components;
using Content.Server.Fluids.EntitySystems;
using Robust.Shared.Prototypes;
using Content.Shared.Sprite;
using Content.Server.Body.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;

namespace Content.Server.Anomaly.Effects;
/// <summary>
/// This component allows the anomaly to chase a random instance of the selected type component within a radius.
/// </summary>
public sealed class LiquidAnomalySystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly PointLightSystem _light = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PuddleSystem _puddle = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly AnomalySystem _anomaly = default!;

    private EntityQuery<InjectableSolutionComponent> _injectableQuery;
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<LiquidAnomalyComponent, AnomalyPulseEvent>(OnPulse);
        SubscribeLocalEvent<LiquidAnomalyComponent, AnomalySupercriticalEvent>(OnSupercritical);
        SubscribeLocalEvent<LiquidAnomalyComponent, AnomalySeverityChangedEvent>(OnSeverityChanged);
        SubscribeLocalEvent<LiquidAnomalyComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<LiquidAnomalyComponent, MobStateChangedEvent>(OnMobStateChanged);

        _injectableQuery = GetEntityQuery<InjectableSolutionComponent>();
    }

    private void OnMapInit(EntityUid uid, LiquidAnomalyComponent component, MapInitEvent args)
    {
        _anomaly.ChangeAnomalyStability(uid, 0.01f); //Very bad code: I do not know how to get severity to call a "GetRandomReagentType" function here
    }

    private void OnPulse(EntityUid uid, LiquidAnomalyComponent component, ref AnomalyPulseEvent args)
    {
        PulseScalableEffect(
            uid,
            component,
            component.MaxSolutionGeneration * args.Severity,
            component.MaxSolutionInjection * args.Severity,
            component.InjectRadius,
            component.Reagent);
    }

    private void OnSupercritical(EntityUid uid, LiquidAnomalyComponent component, ref AnomalySupercriticalEvent args)
    {
        PulseScalableEffect(
            uid,
            component,
            component.SuperCriticalSolutionGeneration,
            component.SuperCriticalSolutionInjection,
            component.SuperCriticalInjectRadius,
            component.Reagent);
    }


    private void OnSeverityChanged(EntityUid uid, LiquidAnomalyComponent component, ref AnomalySeverityChangedEvent args)
    {
        //If after the severity change, its level has exceeded the threshold, the type of reagent changes, and the threshold increases.
        if (args.Severity >= component.NextChangeThreshold)
        {
            component.NextChangeThreshold = args.Severity + component.ReagentChangeStep;

            var reagent = GetRandomReagentType(uid, component, ref args);
            SetReagentType(uid, reagent);
            return;
        }
    }

    /// <summary>
    ///     Change anomaly reagent type.
    /// </summary>
    public void SetReagentType(EntityUid uid, string reagent, LiquidAnomalyComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        if (component.Reagent == reagent)
            return;

        component.Reagent = reagent;

        var color = _proto.Index<ReagentPrototype>(reagent).SubstanceColor;
        _light.SetColor(uid, color);

        //Spawn Effect
        var xform = Transform(uid);
        Spawn(component.VisualEffectPrototype, xform.Coordinates);
        _audio.PlayPvs(component.ChangeSound, uid);

        //Recolor entity
        if (component.NeedRecolorEntity && TryComp<RandomSpriteComponent>(uid, out var randomSprite))
        {
            foreach (var ent in randomSprite.Selected)
            {
                var state = randomSprite.Selected[ent.Key];
                state.Color = color;
                randomSprite.Selected[ent.Key] = state;
            }
            Dirty(uid, randomSprite);
        }
        //Change Bloodstream
        if (component.NeedBloodstreamChange)
            _bloodstream.ChangeBloodReagent(uid, reagent);
    }

    private string GetRandomReagentType(EntityUid uid, LiquidAnomalyComponent component, ref AnomalySeverityChangedEvent args)
    {
        //Category Weight Randomization
        var currentWeightDangerous = Lerp(component.WeightSpreadDangerous.X, component.WeightSpreadDangerous.Y,args.Severity);
        var currentWeightFun = Lerp(component.WeightSpreadFun.X, component.WeightSpreadFun.Y, args.Severity);
        var currentWeightUseful = Lerp(component.WeightSpreadUseful.X, component.WeightSpreadUseful.Y, args.Severity);
        var currentWeightOther = Lerp(component.WeightSpreadOther.X, component.WeightSpreadOther.Y, args.Severity);

        var sumWeight = currentWeightDangerous + currentWeightFun + currentWeightUseful + currentWeightOther;
        var rnd = _random.NextFloat(0f, sumWeight);


        if (rnd <= currentWeightDangerous && component.DangerousChemicals.Count > 0)
        {
            var reagent = _random.Pick(component.DangerousChemicals);
            return reagent;
        }
        else rnd -= currentWeightDangerous;

        if (rnd <= currentWeightFun && component.FunChemicals.Count > 0)
        {
            var reagent = _random.Pick(component.FunChemicals);
            return reagent;
        }
        else rnd -= currentWeightFun;

        if (rnd <= currentWeightUseful && component.UsefulChemicals.Count > 0)
        {
            var reagent = _random.Pick(component.UsefulChemicals);
            return reagent;
        }
        else //Pickup other
        {
            var allReagents = _proto.EnumeratePrototypes<ReagentPrototype>().Select(proto => proto.ID).ToHashSet();

            foreach (var chem in component.BlacklistChemicals)
            {
                allReagents.Remove(chem);
            }
            foreach (var chem in component.DangerousChemicals)
            {
                allReagents.Remove(chem);
            }
            foreach (var chem in component.FunChemicals)
            {
                allReagents.Remove(chem);
            }
            foreach (var chem in component.UsefulChemicals)
            {
                allReagents.Remove(chem);
            }
            if (allReagents.Count == 0) return "Water"; // Error catcher

            var reagent = _random.Pick(allReagents);
            return reagent;
        }
    }

    private void PulseScalableEffect(
        EntityUid uid,
        LiquidAnomalyComponent component,
        float solutionAmount,
        float maxInject,
        float injectRadius,
        string reagent)
    {
        //We get all the entity in the radius into which the reagent will be injected.
        var xformQuery = GetEntityQuery<TransformComponent>();
        var xform = xformQuery.GetComponent(uid);
        var allEnts = _lookup.GetComponentsInRange<InjectableSolutionComponent>(xform.MapPosition, injectRadius)
            .Select(x => x.Owner).ToList();

        //for each matching entity found
        foreach (var ent in allEnts)
        {
            if (!_solutionContainer.TryGetInjectableSolution(ent, out var injectable))
                continue;

            if (_injectableQuery.TryGetComponent(ent, out var injEnt))
            {
                _solutionContainer.TryAddReagent(ent, injectable, reagent, maxInject, out var accepted);

                //Spawn Effect
                var uidXform = Transform(ent);
                Spawn(component.VisualEffectPrototype, uidXform.Coordinates);
            }
        }

        //Create Puddle
        Solution solution = new();
        solution.AddReagent(reagent, solutionAmount);
        _puddle.TrySpillAt(uid, solution, out _);
    }

    //If the component is connected to a mob, When this mob dies, a supercritical effect is activated
    private void OnMobStateChanged(EntityUid uid, LiquidAnomalyComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
        {
            Spawn(component.VisualEffectPrototype, Transform(uid).Coordinates);
            _anomaly.DoAnomalySupercriticalEvent(uid);
        }
    }

    private float Lerp(float a, float b, float t) //maybe this function is in the engine, but I didn't find it
    {
        t = (t < 0) ? 0 : (t > 1) ? 1 : t;
        return a + (b - a) * t;
    }
}
