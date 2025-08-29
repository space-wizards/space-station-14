using System;
using System.Linq;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Chat.Managers;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Lightning;
using Content.Server.Radio.EntitySystems;
using Content.Server.Starlight.Energy.Supermatter;
using Content.Shared.Abilities.Goliath;
using Content.Shared.Atmos;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Ghost;
using Content.Shared.Interaction;
using Content.Shared.Projectiles;
using Content.Shared.Radiation.Components;
using Content.Shared.Radio;
using Content.Shared.Singularity.Components;
using Content.Shared.Starlight.Antags.Abductor;
using Content.Shared.Starlight.Energy.Supermatter;
using Microsoft.CodeAnalysis;
using Robust.Server.Audio;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Toolshed.TypeParsers;

namespace Content.Server.Starlight.Energy.Supermatter;

public sealed class SupermatterSystem : AccUpdateEntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly LightningSystem _lightning = default!;
    [Dependency] private readonly RadioSystem _radioSystem = default!;
    [Dependency] private readonly SupermatterCascadeSystem _cascade = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;

    private readonly Dictionary<EntityUid, Entity<SupermatterComponent>> _supermatters = [];
    private DamageGroupPrototype? _brute;
    private DamageGroupPrototype? _burn;
    private RadioChannelPrototype? _engi;
    public override void Initialize()
    {
        SubscribeLocalEvent<SupermatterComponent, ComponentInit>(AddSupermatter);
        SubscribeLocalEvent<SupermatterComponent, ComponentShutdown>(RemoveSupermatter);

        SubscribeLocalEvent<SupermatterComponent, EndCollideEvent>(OnCollide);
        SubscribeLocalEvent<SupermatterComponent, InteractHandEvent>(OnInteract);
    }

    private void OnInteract(Entity<SupermatterComponent> ent, ref InteractHandEvent args)
    {
        if (HasComp<GhostComponent>(args.User)) return;

        _audio.PlayPvs(Const.AudioEvaporate, ent.Owner);

        float damage = 1;
        if (TryComp<FixturesComponent>(args.User, out var fixture))
            damage = fixture.Fixtures.Select(x => x.Value.Density).Aggregate((i, p) => p + i) / 3;

        _burn ??= _prototypes.Index<DamageGroupPrototype>("Burn");
        _damageable.TryChangeDamage(ent.Owner, new(_burn, damage), true);

        QueueDel(args.User);
    }

    private void OnCollide(Entity<SupermatterComponent> ent, ref EndCollideEvent args)
    {
        ent.Comp.Activated = true;

        if (HasComp<ProjectileComponent>(args.OtherEntity)
        || HasComp<SingularityComponent>(args.OtherEntity)) return;

        _audio.PlayPvs(Const.AudioEvaporate, ent.Owner);
        float damage = 1;
        if (TryComp<FixturesComponent>(args.OtherEntity, out var fixture))
            damage = fixture.Fixtures.Select(x => x.Value.Density).Aggregate((i, p) => p + i) / 3;

        _burn ??= _prototypes.Index<DamageGroupPrototype>("Burn");
        _damageable.TryChangeDamage(ent.Owner, new(_burn, damage), true);

        QueueDel(args.OtherEntity);
    }
    private void AddSupermatter(Entity<SupermatterComponent> ent, ref ComponentInit args) => _supermatters.TryAdd(ent.Owner, ent);
    private void RemoveSupermatter(Entity<SupermatterComponent> ent, ref ComponentShutdown args) => _supermatters.Remove(ent.Owner);

    protected override float Threshold { get; set; } = 1f;
    protected override void AccUpdate()
    {
        foreach (var supermatter in _supermatters)
            Handle(supermatter.Value);
    }

    private void Handle(Entity<SupermatterComponent> supermatter)
    {
        if (!supermatter.Comp.Activated) return;

        HandleDamage(supermatter);
        HandleGas(supermatter);
        HandleRadiation(supermatter);
        HandleLighting(supermatter);
        HandleDestruction(supermatter);
        NotifyCascad(supermatter);
        Cascad(supermatter);
    }

    private void Cascad(Entity<SupermatterComponent> supermatter)
    {
        if (supermatter.Comp.Durability > 0.01) return;
        _explosion.QueueExplosion(supermatter, ExplosionSystem.DefaultExplosionPrototypeId, 150, 3, 20);
        _cascade.StartCascade(Transform(supermatter.Owner).Coordinates);
        QueueDel(supermatter.Owner);
    }

    private void NotifyCascad(Entity<SupermatterComponent> supermatter)
    {
        var currentDurability = (int)Math.Floor(supermatter.Comp.Durability.Float());
        var lastDurability = (int)Math.Floor(supermatter.Comp.LastSendedDurability.Float());
        _engi ??= _prototypes.Index<RadioChannelPrototype>("Engineering");

        if (Math.Abs(currentDurability - lastDurability) < 5)
            return;

        supermatter.Comp.LastSendedDurability = supermatter.Comp.Durability;

        if (currentDurability > lastDurability)
            _radioSystem.SendRadioMessage(supermatter.Owner, $"The crystal is regenerating. Durability: {currentDurability}%", _engi, supermatter.Owner);
        else switch (currentDurability)
            {
                case > 75: _radioSystem.SendRadioMessage(supermatter.Owner, $"Attention! The crystal is destabilizing. Durability: {currentDurability}%", _engi, supermatter.Owner); break;
                case > 50: _chat.DispatchServerAnnouncement($"Attention! The crystal is destabilizing. Durability: {currentDurability}%", Color.Yellow); break;
                case > 25: _chat.DispatchServerAnnouncement($"Critical state of the crystal! Durability: {currentDurability}%", Color.OrangeRed); break;
                default: _chat.DispatchServerAnnouncement($"Crystal destruction is inevitable. Current durability: {currentDurability}%", Color.Red); break;
            }
    }

    private void HandleDestruction(Entity<SupermatterComponent> supermatter)
    {
        var damageToApply = MathHelper.Clamp((supermatter.Comp.AccBreak / 10) - Const.RegenerationPerSecond, -Const.RegenerationPerSecond, Const.MaxDamagePerSecond);
        supermatter.Comp.AccBreak = 0;

        supermatter.Comp.Durability = MathHelper.Clamp(supermatter.Comp.Durability - damageToApply, 0f, 100f);
    }

    private void HandleLighting(Entity<SupermatterComponent> supermatter)
    {
        if (supermatter.Comp.AccLighting != 0
            && _lightning.ShootRandomLightnings(supermatter.Owner, supermatter.Comp.AccLighting.Float(), 1))
            supermatter.Comp.AccLighting = 0;
    }

    private void HandleRadiation(Entity<SupermatterComponent> supermatter)
    {
        var radComp = EnsureComp<RadiationSourceComponent>(supermatter.Owner);
        radComp.Intensity = supermatter.Comp.AccRadiation.Float();

        supermatter.Comp.AccRadiation /= supermatter.Comp.RadiationStability;
    }

    private void HandleGas(Entity<SupermatterComponent> supermatter)
    {
        var gas = _atmosphere.GetTileMixture(supermatter.Owner, true) ?? new();
        DamageByPressure(supermatter, gas);
        DamageByTemperature(supermatter, gas);

        if (gas.TotalMoles < 1) return;

        float heatTransfer = 0;
        float heatModifier = 0;
        float radiationStability = 0;

        for (var i = 0; i < Const.GasProperties.Length; i++)
        {
            var prop = Const.GasProperties[i];
            var percent = Math.Clamp(gas.Moles[i] / gas.TotalMoles, 0, 1);
            heatTransfer += prop.HeatTransferPerMole * gas.Moles[i];
            heatModifier += prop.HeatModifier * percent;
            radiationStability += prop.RadiationStability * percent;
        }

        supermatter.Comp.RadiationStability = MathHelper.Clamp(radiationStability, 1.1, 10);

        ProcessHeat(supermatter, gas, heatTransfer, heatModifier);
        TryCompensateDamage(supermatter, gas);
    }

    private static void TryCompensateDamage(Entity<SupermatterComponent> supermatter, GasMixture gas)
    {
        var breakDelta = supermatter.Comp.AccBreak > Const.EvaporationCompensation ? Const.EvaporationCompensation : supermatter.Comp.AccBreak;
        if (breakDelta == 0) return;
        supermatter.Comp.AccBreak -= breakDelta;

        gas.AdjustMoles((int)Gas.Tritium, breakDelta.Float()/2);
        
        gas.AdjustMoles((int)Gas.Oxygen, breakDelta.Float()*4);
    }

    private static void ProcessHeat(Entity<SupermatterComponent> supermatter, GasMixture gas, float heatTransfer, float heatModifier)
    {
        var accHeat = supermatter.Comp.AccHeat.Float();
        var heatDelta = heatTransfer <= accHeat ? heatTransfer : accHeat;

        accHeat = MathHelper.Clamp(accHeat - heatDelta, 0, 9999); ;
        supermatter.Comp.AccHeat = 0;
        gas.Temperature += heatDelta * heatModifier;
        supermatter.Comp.AccBreak += accHeat;
    }

    private void DamageByTemperature(Entity<SupermatterComponent> supermatter, GasMixture gas)
    {
        if (gas.Temperature <= Const.MaxTemperature) return;
        _audio.PlayPvs(_random.Pick(Const.AudioBurn), supermatter.Owner);
        _burn ??= _prototypes.Index<DamageGroupPrototype>("Burn");
        DamageSpecifier damage = new(_burn, Const.MaxTemperature - gas.Temperature);
        _damageable.TryChangeDamage(supermatter.Owner, damage, true);
    }

    private void DamageByPressure(Entity<SupermatterComponent> supermatter, GasMixture gas)
    {
        if (gas.Pressure >= Const.MinPressure && gas.Pressure <= Const.MaxPressure) return;
        _audio.PlayPvs(_random.Pick(Const.AudioCrack), supermatter.Owner);
        _brute ??= _prototypes.Index<DamageGroupPrototype>("Brute");
        DamageSpecifier damage = new(_brute, Math.Max(Const.MinPressure - gas.Pressure, gas.Pressure - Const.MaxPressure) / 100);
        _damageable.TryChangeDamage(supermatter.Owner, damage, true);
    }

    private void HandleDamage(Entity<SupermatterComponent> supermatter)
    {
        EnsureComp<DamageableComponent>(supermatter.Owner, out var damageable);
        var trueDamage = damageable.TotalDamage * Const.DamageMultiplayer;
        _damageable.TryChangeDamage(supermatter.Owner, damageable.Damage.Invert(), true);

        supermatter.Comp.AccBreak = MathHelper.Clamp(supermatter.Comp.AccBreak + (trueDamage * Const.BreakPercent), 0, 9999);
        supermatter.Comp.AccHeat = MathHelper.Clamp(supermatter.Comp.AccHeat + (trueDamage * Const.HeatPercent), 0, 9999);
        supermatter.Comp.AccLighting = MathHelper.Clamp(supermatter.Comp.AccLighting + (trueDamage * Const.LightingPercent), 0, 25);
        supermatter.Comp.AccRadiation = MathHelper.Clamp(supermatter.Comp.AccRadiation + (trueDamage * Const.RadiationPercent), 0, 50);
    }
}