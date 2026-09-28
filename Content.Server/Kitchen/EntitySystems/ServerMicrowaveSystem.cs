using Content.Server.Administration.Logs;
using Content.Server.Construction;
using Content.Server.Construction.Components;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Lightning;
using Content.Shared.Damage.Components;
using Content.Shared.Database;
using Content.Shared.Interaction.Events;
using Content.Shared.Kitchen.Components;
using Content.Shared.Kitchen.EntitySystems;
using Content.Shared.Suicide;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Kitchen.EntitySystems;

/// <inheritdoc />
public sealed partial class ServerMicrowaveSystem : MicrowaveSystem
{
    [Dependency] private IAdminLogManager _adminLogger = default!;
    [Dependency] private ExplosionSystem _explosion = default!;
    [Dependency] private LightningSystem _lightning = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedSuicideSystem _suicide = default!;

    [Dependency] private EntityQuery<DamageableComponent> _damageableQuery;
    [Dependency] private EntityQuery<MachineComponent> _machineQuery;

    /// <summary>
    /// Kills the user by microwaving their head.
    /// </summary>
    /// <remarks>
    /// TODO: Make this not awful, it keeps any items attached to your head still on and you can
    /// revive someone and cogni them so you have some dumb headless fuck running around. I've seen it happen.
    /// </remarks>
    [SubscribeLocalEvent]
    private void OnSuicideByEnvironment(Entity<MicrowaveComponent> ent, ref SuicideByEnvironmentEvent args)
    {
        if (args.Handled)
            return;

        // The act of getting your head microwaved doesn't actually kill you
        if (!_damageableQuery.TryComp(args.Victim, out var damageableComponent))
            return;

        // The application of lethal damage is what kills you...
        _suicide.ApplyLethalDamage((args.Victim, damageableComponent), "Heat");

        var victim = args.Victim;
        var othersMessage = Loc.GetString("microwave-component-suicide-others-message", ("victim", victim));
        var selfMessage = Loc.GetString("microwave-component-suicide-message");

        Popup.PopupEntity(othersMessage, victim, Filter.PvsExcept(victim), true);
        Popup.PopupEntity(selfMessage, victim, victim);

        var audioParams = ent.Comp.ClickSound.Params;
        Audio.PlayPvs(ent.Comp.ClickSound, ent.Owner, audioParams);

        ent.Comp.CurrentCookTimerTime = 10;
        DirtyField(ent.AsNullable(), nameof(ent.Comp.CurrentCookTimeButtonIndex));
        StartCooking(ent, args.Victim);
        UpdateUI(ent.AsNullable());
        args.Handled = true;
    }

    /// <summary>
    /// Prevents construction graph operations as a result of temperature changes.
    /// </summary>
    /// <remarks>
    /// For example: raw meat will not turn into steak while it is actively being microwaved.
    /// </remarks>
    [SubscribeLocalEvent]
    private void OnConstructionTemp(Entity<ActivelyMicrowavedComponent> ent, ref OnConstructionTemperatureEvent args)
    {
        args.Result = HandleResult.False;
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
    public override void Explode(Entity<MicrowaveComponent?> ent)
    {
        base.Explode(ent);

        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        _explosion.TriggerExplosive(ent);

        if (_machineQuery.TryComp(ent, out var machine))
        {
            Container.CleanContainer(machine.BoardContainer);
            Container.EmptyContainer(machine.PartContainer);
        }

        _adminLogger.Add(LogType.Action,
            LogImpact.Medium,
            $"{ToPrettyString(ent)} exploded from unsafe cooking!");
    }
}
