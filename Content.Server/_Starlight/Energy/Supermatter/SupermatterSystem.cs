using Content.Server.Atmos.EntitySystems;
using Content.Server.Starlight.Energy.Supermatter;
using Content.Shared.Abilities.Goliath;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Starlight.Antags.Abductor;
using Content.Shared.Starlight.Energy.Supermatter;
using Robust.Server.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Starlight.Energy.Supermatter;

public sealed class SupermatterSystem : AccUpdateEntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private readonly Dictionary<EntityUid, Entity<SupermatterComponent>> _supermatters = [];
    public override void Initialize()
    {
        SubscribeLocalEvent<SupermatterComponent, ComponentStartup>(AddSupermatter);
        SubscribeLocalEvent<SupermatterComponent, ComponentShutdown>(RemoveSupermatter);
    }

    private void AddSupermatter(Entity<SupermatterComponent> ent, ref ComponentStartup args) => _supermatters.TryAdd(ent.Owner, ent);
    private void RemoveSupermatter(Entity<SupermatterComponent> ent, ref ComponentShutdown args) => _supermatters.Remove(ent.Owner);

    protected override float Threshold { get; set; } = 1f;
    protected override void AccUpdate()
    {
        foreach (var supermatter in _supermatters)
            Handle(supermatter.Value);
    }

    private void Handle(Entity<SupermatterComponent> supermatter)
    {
        HandleDamage(supermatter);
        HandleGas(supermatter);
    }

    private void HandleGas(Entity<SupermatterComponent> supermatter)
    {
        var gas = _atmosphere.GetTileMixture(supermatter.Owner) ?? new();

        if (gas.Pressure < Const.MinPressure || gas.Pressure > Const.MaxPressure)
        {
            _audio.PlayPvs(_random.Pick(Const.AudioCrack), supermatter.Owner);
            DamageSpecifier damage = new(_prototypes.Index<DamageGroupPrototype>("Brute"), Math.Max(Const.MinPressure - gas.Pressure, gas.Pressure - Const.MaxPressure));//todo specifier needs to be reused
            _damageable.TryChangeDamage(supermatter.Owner, damage, true);
        }

        if (gas.Temperature > Const.MaxTemperature)
        {
            _audio.PlayPvs(_random.Pick(Const.AudioBurn), supermatter.Owner);
            DamageSpecifier damage = new(_prototypes.Index<DamageGroupPrototype>("Burn"), Const.MaxTemperature - gas.Temperature);  //todo specifier needs to be reused
            _damageable.TryChangeDamage(supermatter.Owner, damage, true);
        }
        double heatTransfer = 0;

        for (var i = 0; i < Const.GasProperties.Length; i++)
        {
            var prop = Const.GasProperties[i];
            heatTransfer += prop.HeatTransferPerMole * gas.Moles[i];
        }
    }

    private void HandleDamage(Entity<SupermatterComponent> supermatter)
    {
        EnsureComp<DamageableComponent>(supermatter.Owner, out var damageable);
        var trueDamage = damageable.TotalDamage;
        _damageable.TryChangeDamage(supermatter.Owner, damageable.Damage.Invert(), true);

        supermatter.Comp.AccBreak = MathHelper.Clamp(supermatter.Comp.AccBreak + (trueDamage * Const.BreakPercent), 0, 9999);
        supermatter.Comp.AccHeat = MathHelper.Clamp(supermatter.Comp.AccHeat + (trueDamage * Const.HeatPercent), 0, 9999);
        supermatter.Comp.AccLighting = MathHelper.Clamp(supermatter.Comp.AccLighting + (trueDamage * Const.LightingPercent), 0, 9999);
        supermatter.Comp.AccRadiation = MathHelper.Clamp(supermatter.Comp.AccRadiation + (trueDamage * Const.RadiationPercent), 0, 9999);
    }
}