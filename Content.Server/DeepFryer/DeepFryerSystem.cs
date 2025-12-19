using Content.Server.Access.Components;
using Content.Server.Ghost;
using Content.Server.Kitchen.Components;
using Content.Server.Power.Components;
using Content.Server.Temperature.Systems;
using Content.Shared.Administration.Logs;
using Content.Shared.Audio;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Climbing.Events;
using Content.Shared.Construction.Components;
using Content.Shared.Database;
using Content.Shared.DeepFryer;
using Content.Shared.DeepFryer.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Events;
using Content.Shared.Movement.Components;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Storage.Components;
using Content.Shared.Temperature.Components;
using Content.Shared.Throwing;
using JetBrains.FormatRipper.Elf;
using NetCord;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Numerics;

namespace Content.Server.DeepFryer;
public sealed class DeepFryerSystem : SharedDeepFryerSystem
{
    [Dependency] private readonly SharedAmbientSoundSystem _ambientSoundSystem = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly GhostSystem _ghostSystem = default!;
    [Dependency] private readonly TemperatureSystem _temperature = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActiveFryingDeepFryerComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ActiveFryingDeepFryerComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<DeepFryerComponent, SuicideByEnvironmentEvent>(OnSuicideByEnvironment);
        SubscribeLocalEvent<DeepFryerComponent, PowerChangedEvent>(OnPowerChanged);
    }

    private void OnInit(EntityUid uid, ActiveFryingDeepFryerComponent component, ComponentInit args)
    {
        _ambientSoundSystem.SetAmbience(uid, true);
    }

    private void OnShutdown(EntityUid uid, ActiveFryingDeepFryerComponent component, ComponentShutdown args)
    {
        _ambientSoundSystem.SetAmbience(uid, false);
    }

    private void OnPowerChanged(EntityUid uid, DeepFryerComponent component, ref PowerChangedEvent args)
    {
        // Power only counts for heating the vat solution
        //if (args.Powered && TryComp<EntityStorageComponent>(uid, out var storage) && !storage.Open && storage.Contents.Count > 0)
        if (args.Powered)
            EnsureComp<ActiveHeatingDeepFryerComponent>(uid);
        else
            RemComp<ActiveHeatingDeepFryerComponent>(uid);
    }

    // Honestly not sure when this comes into play. Maybe if you try to ghost while inside?
    private void OnSuicideByEnvironment(Entity<DeepFryerComponent> ent, ref SuicideByEnvironmentEvent args)
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
    }

    /// <summary>
    /// Adds temperature to every item in the deep fryer based on vat solution temperature
    /// </summary>
    private void AddTemperature(EntityUid uid, DeepFryerComponent fryer, float time)
    {
        if (TryComp<EntityStorageComponent>(uid, out var storage)
            && _solutionContainer.TryGetSolution(uid, fryer.SolutionName, out var deepFryerSoln, out var deepFryerSolution)
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
                    if (solution.Temperature > fryer.MaxHeat)
                        continue;

                    _solutionContainer.AddThermalEnergy(soln, deepFryerSolution.Temperature);
                }
            }
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<DeepFryerComponent>();
        while (query.MoveNext(out var uid, out var fryer))
        {
            AddTemperature(uid, fryer, frameTime);
        }
    }
}
