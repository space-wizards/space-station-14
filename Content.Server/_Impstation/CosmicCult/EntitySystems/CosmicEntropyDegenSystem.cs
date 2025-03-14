using Content.Shared._Impstation.CosmicCult.Components;
using Robust.Shared.Timing;
using Content.Shared.Damage;
using Content.Shared.Popups;
using Robust.Shared.Random;

namespace Content.Server._Impstation.CosmicCult.EntitySystems;

/// <summary>
/// Makes the person with this component take damage over time.
/// Used for status effect.
/// </summary>
public sealed partial class CosmicEntropyDegenSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<CosmicEntropyDebuffComponent, ComponentStartup>(OnInit);
    }

    private void OnInit(EntityUid uid, CosmicEntropyDebuffComponent comp, ref ComponentStartup args)
    {
        _damageable.TryChangeDamage(uid, comp.Degen, true, false);
        comp.CheckTimer = _timing.CurTime + comp.CheckWait;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CosmicEntropyDebuffComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (_timing.CurTime < component.CheckTimer)
                continue;
            component.CheckTimer = _timing.CurTime + component.CheckWait;
            _damageable.TryChangeDamage(uid, component.Degen, true, false);
            if (_random.Prob(component.PopupChance))
                _popup.PopupEntity(Loc.GetString("entropy-effect-numb"), uid, uid, PopupType.SmallCaution);
        }
    }
}
