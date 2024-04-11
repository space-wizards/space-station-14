using Content.Shared.Changeling.Components;
using Content.Shared.Changeling;
using Content.Shared.Inventory;
using Content.Shared.Interaction.Components;
using Content.Shared.Hands.Components;
using Content.Server.Hands.Systems;
using Robust.Shared.Prototypes;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Server.Body.Systems;
using Content.Shared.Popups;
using Robust.Shared.Player;
using Content.Shared.IdentityManagement;
using Robust.Shared.Audio.Systems;
using Content.Shared.Stealth.Components;
using Content.Server.Emp;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Server.Forensics;
using Content.Shared.FixedPoint;
using Content.Server.Store.Components;
using Content.Shared.Chemistry.Components;
using Content.Server.Fluids.EntitySystems;

namespace Content.Server.Changeling;

public sealed partial class ChangelingSystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly HandsSystem _handsSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly EmpSystem _emp = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly PuddleSystem _puddle = default!;

    private void InitializeLingAbilities()
    {
        SubscribeLocalEvent<ChangelingComponent, LingAbsorbActionEvent>(StartAbsorbing);
        SubscribeLocalEvent<ChangelingComponent, AbsorbDoAfterEvent>(OnAbsorbDoAfter);

        SubscribeLocalEvent<ChangelingComponent, LingRegenerateActionEvent>(OnRegenerate);

        SubscribeLocalEvent<ChangelingComponent, LingStingExtractActionEvent>(OnLingDNASting);
    }

    private void StartAbsorbing(Entity<ChangelingComponent> ent, ref LingAbsorbActionEvent args)
    {
        if (args.Handled)
            return;

        var target = args.Target;

        if (!_mobState.IsIncapacitated(target)) // if target isn't crit or dead dont let absorb
        {
            var selfMessage = Loc.GetString("changeling-dna-fail-notdead", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessage, ent, ent);
            return;
        }

        if (HasComp<AbsorbedComponent>(target))
        {
            var selfMessage = Loc.GetString("changeling-dna-alreadyabsorbed", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessage, ent, ent);
            return;
        }

        args.Handled = true;

        _popup.PopupEntity(Loc.GetString("changeling-dna-stage-1"), ent, ent);

        var doAfter = new DoAfterArgs(EntityManager, ent, 15, new AbsorbDoAfterEvent(), ent, target: target)
        {
            DistanceThreshold = 2,
            BreakOnMove = true,
            BreakOnDamage = true,
            AttemptFrequency = AttemptFrequency.StartAndEnd
        };

        _doAfter.TryStartDoAfter(doAfter);
    }

    public ProtoId<DamageGroupPrototype> GeneticDamageGroup = "Genetic";
    private void OnAbsorbDoAfter(Entity<ChangelingComponent> ent, ref AbsorbDoAfterEvent args)
    {
        if (args.Handled || args.Args.Target == null)
            return;

        var component = ent.Comp;
        args.Handled = true;
        args.Repeat = RepeatDoAfter(component);
        var target = args.Args.Target.Value;

        if (args.Cancelled || !_mobState.IsIncapacitated(target) || HasComp<AbsorbedComponent>(target))
        {
            var selfMessage = Loc.GetString("changeling-dna-interrupted", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessage, ent, ent);
            component.AbsorbStage = 0;
            args.Repeat = false;
            return;
        }

        if (component.AbsorbStage == 0)
        {
            var othersMessage = Loc.GetString("changeling-dna-stage-2-others", ("user", Identity.Entity(ent, EntityManager)));
            _popup.PopupEntity(othersMessage, ent, Filter.PvsExcept(ent), true, PopupType.MediumCaution);

            var selfMessage = Loc.GetString("changeling-dna-stage-2-self");
            _popup.PopupEntity(selfMessage, ent, ent, PopupType.MediumCaution);
        }
        else if (component.AbsorbStage == 1)
        {
            var othersMessage = Loc.GetString("changeling-dna-stage-3-others", ("user", Identity.Entity(ent, EntityManager)), ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(othersMessage, ent, Filter.PvsExcept(ent), true, PopupType.LargeCaution);

            var selfMessage = Loc.GetString("changeling-dna-stage-3-self", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessage, ent, ent, PopupType.LargeCaution);
        }
        else if (component.AbsorbStage == 2)
        {
            // give them 200 genetic damage and remove all of their blood
            var dmg = new DamageSpecifier(_proto.Index(GeneticDamageGroup), 200);
            _damageableSystem.TryChangeDamage(target, dmg);
            _bloodstreamSystem.ChangeBloodReagent(target, "FerrochromicAcid"); // replace target's blood with acid, then spill
            _bloodstreamSystem.SpillAllSolutions(target); // replace target's blood with acid, then spill
            EnsureComp<AbsorbedComponent>(target);

            if (HasComp<ChangelingComponent>(target)) // they were another changeling, give extra evolution points
            {
                var selfMessage = Loc.GetString("changeling-dna-success-ling", ("target", Identity.Entity(target, EntityManager)));
                _popup.PopupEntity(selfMessage, ent, ent, PopupType.Medium);

                if (TryComp<StoreComponent>(ent, out var store))
                {
                    _store.TryAddCurrency(new Dictionary<string, FixedPoint2> { { EvolutionPointsCurrencyPrototype, component.AbsorbedChangelingPointsAmount } }, ent, store);
                    _store.UpdateUserInterface(ent, ent, store);
                }
            }
            else
            {
                var selfMessage = Loc.GetString("changeling-dna-success", ("target", Identity.Entity(target, EntityManager)));
                _popup.PopupEntity(selfMessage, ent, ent, PopupType.Medium);
            }
        }

        if (component.AbsorbStage >= 2)
            component.AbsorbStage = 0;
        else
            component.AbsorbStage += 1;
    }

    private static bool RepeatDoAfter(ChangelingComponent component)
    {
        if (component.AbsorbStage < 2.0)
            return true;
        else
            return false;
    }

    public ProtoId<DamageGroupPrototype> BruteDamageGroup = "Brute";
    public ProtoId<DamageGroupPrototype> BurnDamageGroup = "Burn";
    private void OnRegenerate(Entity<ChangelingComponent> ent, ref LingRegenerateActionEvent args)
    {
        if (args.Handled)
            return;

        var component = ent.Comp;

        if (_mobState.IsDead(ent))
        {
            _popup.PopupEntity(Loc.GetString("changeling-regenerate-fail-dead"), ent, ent);
            return;
        }

        if (_mobState.IsCritical(ent)) // make sure the ling is critical, if not they cant regenerate
        {
            if (!TryUseAbility(ent, component, 10))
                return;

            args.Handled = true;

            var damage_brute = new DamageSpecifier(_proto.Index(BruteDamageGroup), -100);
            var damage_burn = new DamageSpecifier(_proto.Index(BurnDamageGroup), -75);
            _damageableSystem.TryChangeDamage(ent, damage_brute);
            _damageableSystem.TryChangeDamage(ent, damage_burn);
            _bloodstreamSystem.TryModifyBloodLevel(ent, 1000); // give back blood and remove bleeding
            _bloodstreamSystem.TryModifyBleedAmount(ent, -1000);
            _audioSystem.PlayPvs(component.SoundRegenerate, ent);

            var othersMessage = Loc.GetString("changeling-regenerate-others-success", ("user", Identity.Entity(ent, EntityManager)));
            _popup.PopupEntity(othersMessage, ent, Filter.PvsExcept(ent), true, PopupType.MediumCaution);

            var selfMessage = Loc.GetString("changeling-regenerate-self-success");
            _popup.PopupEntity(selfMessage, ent, ent, PopupType.MediumCaution);
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("changeling-regenerate-fail-not-crit"), ent, ent);
        }
    }

    // changeling stings
    private void OnLingDNASting(EntityUid uid, ChangelingComponent component, LingStingExtractActionEvent args)
    {
        if (args.Handled)
            return;

        var target = args.Target;

        if (!TryStingTarget(uid, target, component))
            return;

        if (HasComp<AbsorbedComponent>(target))
        {
            var selfMessageFailNoDna = Loc.GetString("changeling-dna-sting-fail-nodna", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessageFailNoDna, uid, uid);
            return;
        }

        var dnaCompTarget = EnsureComp<DnaComponent>(target);

        foreach (var storedData in component.StoredDNA)
        {
            if (storedData.Dna != null && storedData.Dna == dnaCompTarget.DNA)
            {
                var selfMessageFailAlreadyDna = Loc.GetString("changeling-dna-sting-fail-alreadydna", ("target", Identity.Entity(target, EntityManager)));
                _popup.PopupEntity(selfMessageFailAlreadyDna, uid, uid);
                return;
            }
        }

        if (!TryUseAbility(uid, component, 25))
            return;

        if (StealDNA(uid, target, component))
        {
            args.Handled = true;

            var selfMessageSuccess = Loc.GetString("changeling-dna-sting", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessageSuccess, uid, uid);
        }
    }
}
