using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.DoAfter;
using Content.Server.Medical.Components;
using Content.Server.Popups;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.DoAfter;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Medical;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Server.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.Medical;

/// <summary>
/// This handles cauterizing.
/// </summary>
public sealed class CauterizerSystem : EntitySystem
{
    [Dependency] private readonly EntityManager _entman = default!;
    [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly AudioSystem _audio = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<CauterizerComponent, GetVerbsEvent<UtilityVerb>>(OnUtilityVerb);
        SubscribeLocalEvent<CauterizerComponent, CauterizeDoAfterEvent>(OnCauterizeDoAfter);
    }

    private void OnUtilityVerb(Entity<CauterizerComponent> entity, ref GetVerbsEvent<UtilityVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var user = args.User;
        var target = args.Target;

        if (!TryComp<BloodstreamComponent>(target, out _))
            return;

        // check that a heating item is activated. Otherwise a turned off welder would be usable, which is bad
        if (TryComp<ItemToggleHotComponent>(entity, out var toggleHotComponent))
        {
            // if item is hot when toggled, it should probably be toggleable, but can never be too sure
            if (!TryComp<ItemToggleComponent>(entity, out var toggleComponent))
                return;
            if (!toggleComponent.Activated)
                return;
        }

        var verb = new UtilityVerb()
        {
            Act = () => TryCauterize(entity, user, target),
            Text = Loc.GetString("cauterize-verb-name"),
            Message = Loc.GetString("cauterize-verb-message"),
        };

        args.Verbs.Add(verb);
    }

    private bool TryCauterize(Entity<CauterizerComponent> entity, EntityUid user, EntityUid target)
    {
        if (!TryComp<BloodstreamComponent>(target, out var bloodstream))
        {
            _popupSystem.PopupEntity(Loc.GetString("cauterize-verb-no-bloodstream", ("target", target)), user, user, PopupType.Medium);
            return false;
        }

        if (bloodstream.BleedAmount <= 0)
        {
            _popupSystem.PopupEntity(Loc.GetString("cauterize-verb-not-bleeding", ("target", target)), user, user, PopupType.Medium);
            return false;
        }

        var cauterizeDuration = entity.Comp.DoAfterDuration;
        var doAfterArgs = new DoAfterArgs(_entman,
            user,
            cauterizeDuration,
            new CauterizeDoAfterEvent(),
            entity,
            target,
            entity)
        {
            BreakOnHandChange = true,
            NeedHand = true,
            BreakOnDamage = true,
            BreakOnMove = true,
            MovementThreshold = 0.01f,
            DistanceThreshold = entity.Comp.Distance,
        };

        _doAfterSystem.TryStartDoAfter(doAfterArgs);
        _popupSystem.PopupEntity(Loc.GetString("cauterize-verb-start-doafter", ("item", entity.Owner)), user, user);

        return true;
    }

    private void OnCauterizeDoAfter(Entity<CauterizerComponent> ent, ref CauterizeDoAfterEvent args)
    {
        var target = args.Args.Target;
        var bleedreduce = ent.Comp.BleedReduce;
        var heatdmg = ent.Comp.Damage;

        if (args.Handled || args.Cancelled || target == null)
            return;

        if (!TryComp<BloodstreamComponent>(target, out var bloodstream))
            return;

        _bloodstreamSystem.TryModifyBleedAmount(target.Value, bleedreduce, bloodstream);
        _damageableSystem.TryChangeDamage(target,
            new DamageSpecifier(_protoMan.Index<DamageTypePrototype>("Heat"), heatdmg));
        _popupSystem.PopupClient(Loc.GetString("cauterize-verb-succeed"), target, PopupType.Medium);
        _audio.PlayPvs(bloodstream.BloodHealedSound, ent);
    }
}
