using Content.Shared.Starlight.Antags.Abductor;
using Content.Shared.Starlight.Medical.Surgery;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Effects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Spawners;
using Robust.Server.GameObjects;
using Content.Shared.Interaction;
using Content.Shared.Weapons.Melee.Events;
using System.Linq;
using Content.Shared.Tag;
using Content.Shared.Popups;
using System;
using Robust.Shared.Timing;
using Content.Shared.Damage;
using Robust.Shared.Toolshed.TypeParsers;
using Content.Shared.Damage.Prototypes;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Server.Chat.Systems;

namespace Content.Server.Starlight.Antags.Abductor;

public sealed partial class AbductorSystem : SharedAbductorSystem
{
    [Dependency] private readonly IGameTiming _time = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    private float _delayAccumulator = 0f;
    private readonly Stopwatch _stopwatch = new();
    private readonly DamageSpecifier _passiveHealing = new();

    public void InitializeOrgans()
    {
        foreach (var specif in _prototypes.EnumeratePrototypes<DamageTypePrototype>())
            _passiveHealing.DamageDict.Add(specif.ID, -3);
        _stopwatch.Start();
    }

    public override void Update(float frameTime)
    {
        _delayAccumulator += frameTime;
        if (_delayAccumulator > 3)
        {
            _delayAccumulator = 0;
            _stopwatch.Restart();
            var query = EntityQueryEnumerator<AbductorVictimComponent>();
            while (query.MoveNext(out var uid, out var victim) && _stopwatch.Elapsed < TimeSpan.FromMilliseconds(0.5))
            {
                if(victim.Organ != AbductorOrganType.None)
                    Do(uid, victim);
            }
        }
    }

    private void Do(EntityUid uid, AbductorVictimComponent victim)
    {
        switch (victim.Organ)
        {
            case AbductorOrganType.Health:
                if(_time.CurTime - victim.LastActivation < TimeSpan.FromSeconds(3))
                    return;
                victim.LastActivation = _time.CurTime;
                _damageable.TryChangeDamage(uid, _passiveHealing);
                break;
            case AbductorOrganType.Plasma:
                if (_time.CurTime - victim.LastActivation < TimeSpan.FromSeconds(120))
                    return;
                victim.LastActivation = _time.CurTime;
                var mix = _atmos.GetContainingMixture((uid, Transform(uid)), true, true) ?? new();
                mix.AdjustMoles(Gas.Plasma, 30);
                _chat.TryEmoteWithChat(uid, "Cough");
                break;
            case AbductorOrganType.Gravity:
                if (_time.CurTime - victim.LastActivation < TimeSpan.FromSeconds(60))
                    return;
                victim.LastActivation = _time.CurTime;
                var gravity = SpawnAttachedTo("AdminInstantEffectGravityWell", Transform(uid).Coordinates);
                _xformSys.SetParent(gravity, uid);
                break;
            case AbductorOrganType.Egg:
                if (_time.CurTime - victim.LastActivation < TimeSpan.FromSeconds(120))
                    return;
                victim.LastActivation = _time.CurTime;
                SpawnAttachedTo("FoodEggChickenFertilized", Transform(uid).Coordinates);
                break;
            case AbductorOrganType.Spider:
                if (_time.CurTime - victim.LastActivation < TimeSpan.FromSeconds(240))
                    return;
                victim.LastActivation = _time.CurTime;
                SpawnAttachedTo("EggSpiderFertilized", Transform(uid).Coordinates);
                break;
            default:
                break;
        }
    }
}
