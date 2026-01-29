//using Content.Server.Ghost;
using Content.Server.Temperature.Systems;
//using Content.Shared.Administration.Logs;
using Content.Shared.Audio;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.DeepFryer;
using Content.Shared.DeepFryer.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Components;
using Content.Shared.Power;
using Content.Shared.Storage.Components;
using Content.Shared.Temperature.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.DeepFryer;
public sealed class DeepFryerSystem : SharedDeepFryerSystem
{
    [Dependency] private readonly SharedAmbientSoundSystem _ambientSoundSystem = default!;
    //[Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    //[Dependency] private readonly GhostSystem _ghostSystem = default!;
    [Dependency] private readonly TemperatureSystem _temperature = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActiveFryingDeepFryerComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ActiveFryingDeepFryerComponent, ComponentShutdown>(OnShutdown);
        //SubscribeLocalEvent<DeepFryerComponent, SuicideByEnvironmentEvent>(OnSuicideByEnvironment);
        SubscribeLocalEvent<DeepFryerComponent, PowerChangedEvent>(OnPowerChanged);
    }

    private void OnInit(Entity<ActiveFryingDeepFryerComponent> ent, ref ComponentInit args)
    {
        _ambientSoundSystem.SetAmbience(ent.Owner, true);
    }

    private void OnShutdown(Entity<ActiveFryingDeepFryerComponent> ent, ref ComponentShutdown args)
    {
        _ambientSoundSystem.SetAmbience(ent.Owner, false);
    }

    private void OnPowerChanged(Entity<DeepFryerComponent> ent, ref PowerChangedEvent args)
    {
        // Power only counts for heating the vat solution
        if (args.Powered)
            EnsureComp<ActiveHeatingDeepFryerComponent>(ent.Owner);
        else
            RemComp<ActiveHeatingDeepFryerComponent>(ent.Owner);
    }

    // Crematorium had this, but I have no idea how to make it work.
    /*private void OnSuicideByEnvironment(Entity<DeepFryerComponent> ent, ref SuicideByEnvironmentEvent args)
    {
        if (args.Handled)
            return;

        var victim = args.Victim;
        if (HasComp<ActorComponent>(victim) && Mind.TryGetMind(victim, out var mindId, out var mind))
        {
            _ghostSystem.OnGhostAttempt(mindId, false, mind: mind);

            if (mind.OwnedEntity is { Valid: true } entity)
            {
                Popup.PopupEntity(Loc.GetString("crematorium-entity-storage-component-suicide-message"), entity);
            }
        }

        Popup.PopupEntity(Loc.GetString("crematorium-entity-storage-component-suicide-message-others",
            ("victim", Identity.Entity(victim, EntityManager))),
            victim,
            Filter.PvsExcept(victim),
            true,
            PopupType.LargeCaution);

        if (EntityStorage.CanInsert(victim, ent.Owner))
        {
            EntityStorage.CloseStorage(ent.Owner);
            Standing.Down(victim, false);
            EntityStorage.Insert(victim, ent.Owner);
        }
        args.Handled = true;
    }*/

    /// <summary>
    /// Adds temperature to every item in the deep fryer based on vat solution temperature
    /// </summary>
    private void AddTemperature(Entity<DeepFryerComponent?> ent, float time)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (TryComp<EntityStorageComponent>(ent.Owner, out var storage)
            && _solutionContainer.TryGetSolution(ent.Owner, ent.Comp.SolutionName, out var deepFryerSoln, out var deepFryerSolution)
            && deepFryerSolution.Volume != 0)
        {
            foreach (var entity in storage.Contents.ContainedEntities)
            {
                if (TryComp<TemperatureComponent>(entity, out var tempComp))
                    _temperature.ChangeHeat(entity, deepFryerSolution.Temperature, false, tempComp);

                if (!TryComp<SolutionContainerManagerComponent>(entity, out var solutions))
                    continue;
                foreach (var (_, soln) in _solutionContainer.EnumerateSolutions((entity, solutions)))
                {
                    var solution = soln.Comp.Solution;
                    if (solution.Temperature > ent.Comp.MaxHeat)
                        continue;

                    _solutionContainer.AddThermalEnergy(soln, deepFryerSolution.Temperature);
                }
            }
        }
    }

    /// <summary>
    /// Adds heat damage to creatures in heating deep fryers
    /// </summary>
    private void AddHeatDamage(Entity<DeepFryerComponent?> ent, float time)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (TryComp<EntityStorageComponent>(ent.Owner, out var storage))
        {
            ProtoId<DamageTypePrototype> damageId = "Heat";
            DamageTypePrototype heatProto = _prototypeManager.Index(damageId);
            foreach (var entity in storage.Contents.ContainedEntities)
            {
                // Creatures only, so you can't burn the food to ash
                if (TryComp<DamageableComponent>(entity, out var damageable) && HasComp<MobThresholdsComponent>(entity))
                {
                    _damageable.TryChangeDamage(entity, new DamageSpecifier(heatProto, FixedPoint2.New(ent.Comp.HeatingDamage * time)));
                }
            }
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query1 = EntityQueryEnumerator<DeepFryerComponent>();
        while (query1.MoveNext(out var uid, out var fryer))
        {
            AddTemperature(uid, frameTime);
        }
        var query2 = EntityQueryEnumerator<ActiveHeatingDeepFryerComponent, DeepFryerComponent>();
        while (query2.MoveNext(out var uid, out _, out var fryer))
        {
            AddHeatDamage(uid, frameTime);
        }
    }
}
