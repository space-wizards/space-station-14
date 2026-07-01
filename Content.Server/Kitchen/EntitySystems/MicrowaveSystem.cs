using Content.Server.Administration.Logs;
using Content.Server.Construction;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Temperature.Systems;
using Content.Shared.Database;
using Content.Shared.Interaction.Events;
using Robust.Shared.Random;
using Robust.Shared.Audio;
using Content.Server.Lightning;
using Content.Shared.Kitchen.Components;
using Robust.Shared.Player;
using Content.Server.Construction.Components;
using Content.Shared.Chat;
using Content.Shared.Damage.Components;
using Content.Shared.Kitchen.EntitySystems;

namespace Content.Server.Kitchen.EntitySystems;

/// <inheritdoc />
public sealed partial class MicrowaveSystem : SharedMicrowaveSystem
{
    [Dependency] private IAdminLogManager _adminLogger = default!;
    [Dependency] private ExplosionSystem _explosion = default!;
    [Dependency] private LightningSystem _lightning = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedSuicideSystem _suicide = default!;
    [Dependency] private TemperatureSystem _temperature = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MicrowaveComponent, SuicideByEnvironmentEvent>(OnSuicideByEnvironment);
        SubscribeLocalEvent<ActivelyMicrowavedComponent, OnConstructionTemperatureEvent>(OnConstructionTemp);
    }

    /// <summary>
    ///     Kills the user by microwaving their head.
    /// </summary>
    /// <remarks>
    ///     TODO: Make this not awful, it keeps any items attached to your head still on and you can
    ///     revive someone and cogni them so you have some dumb headless fuck running around. I've seen it happen.
    /// </remarks>
    /// <param name="ent">The microwave entity.</param>
    private void OnSuicideByEnvironment(Entity<MicrowaveComponent> ent, ref SuicideByEnvironmentEvent args)
    {
        if (args.Handled)
            return;

        // The act of getting your head microwaved doesn't actually kill you
        if (!TryComp<DamageableComponent>(args.Victim, out var damageableComponent))
            return;

        // The application of lethal damage is what kills you...
        _suicide.ApplyLethalDamage((args.Victim, damageableComponent), "Heat");

        var victim = args.Victim;
        var othersMessage = Loc.GetString("microwave-component-suicide-others-message", ("victim", victim));
        var selfMessage = Loc.GetString("microwave-component-suicide-message");

        PopupSys.PopupEntity(othersMessage, victim, Filter.PvsExcept(victim), true);
        PopupSys.PopupEntity(selfMessage, victim, victim);

        AudioSys.PlayPvs(ent.Comp.ClickSound, ent.Owner, AudioParams.Default.WithVolume(-2));
        ent.Comp.CurrentCookTimerTime = 10;
        StartCooking(ent, args.Victim);
        UpdateUserInterfaceState(ent.AsNullable());
        args.Handled = true;
    }

    /// <summary>
    ///     Prevents construction graph operations as a result of temperature changes.
    /// </summary>
    /// <remarks>
    ///     For example: raw meat will not turn into steak while it is actively being microwaved.
    /// </remarks>
    /// <param name="ent">An entity that is actively being microwaved.</param>
    private void OnConstructionTemp(Entity<ActivelyMicrowavedComponent> ent, ref OnConstructionTemperatureEvent args)
    {
        args.Result = HandleResult.False;
    }

    /// <inheritdoc />
    protected override void AddTemperature(Entity<MicrowaveComponent> ent, float time)
    {
        var component = ent.Comp;
        var heatToAdd = time * component.BaseHeatMultiplier;
        var objHeatToAdd = heatToAdd * component.ObjectHeatMultiplier;

        foreach (var entity in component.Storage.ContainedEntities)
        {
            _temperature.ChangeHeat(entity, objHeatToAdd, ignoreHeatResistance: false);

            foreach (var (_, soln) in SolutionSys.EnumerateSolutions(entity))
            {
                var solution = soln.Comp.Solution;
                if (solution.Temperature > component.TemperatureUpperThreshold)
                    continue;

                SolutionSys.AddThermalEnergy(soln, heatToAdd);
            }
        }
    }

    /// <inheritdoc />
    protected override void RollMalfunction(Entity<MicrowaveComponent> ent)
    {
        base.RollMalfunction(ent);

        // this microwave sploded
        if (ent.Comp.Broken)
            return;

        var comp = ent.Comp;
        if (_random.Prob(comp.LightningChance))
            _lightning.ShootRandomLightnings(ent, 1.0f, 2, comp.MalfunctionSpark, triggerLightningEvents: false);
    }

    /// <summary>
    /// Explodes the microwave internally, turning it into a broken state, destroying its board, and spitting out its machine parts
    /// </summary>
    /// <param name="ent">The microwave entity.</param>
    protected override void Explode(Entity<MicrowaveComponent> ent)
    {
        base.Explode(ent);

        _explosion.TriggerExplosive(ent);

        if (TryComp<MachineComponent>(ent, out var machine))
        {
            ContainerSys.CleanContainer(machine.BoardContainer);
            ContainerSys.EmptyContainer(machine.PartContainer);
        }

        _adminLogger.Add(LogType.Action, LogImpact.Medium,
            $"{ToPrettyString(ent)} exploded from unsafe cooking!");
    }
}
