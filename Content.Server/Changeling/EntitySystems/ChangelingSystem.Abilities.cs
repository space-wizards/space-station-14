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

namespace Content.Server.Changeling.EntitySystems;

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
        SubscribeLocalEvent<ChangelingComponent, ArmBladeActionEvent>(OnArmBladeAction);
        SubscribeLocalEvent<ChangelingComponent, LingArmorActionEvent>(OnLingArmorAction);
        SubscribeLocalEvent<ChangelingComponent, LingInvisibleActionEvent>(OnLingInvisible);
        SubscribeLocalEvent<ChangelingComponent, LingEMPActionEvent>(OnLingEmp);
        SubscribeLocalEvent<ChangelingComponent, LingStingExtractActionEvent>(OnLingDNASting);
    }

    private void StartAbsorbing(EntityUid uid, ChangelingComponent component, LingAbsorbActionEvent args)
    {
        if (args.Handled)
            return;

        var target = args.Target;
        if (!HasComp<HumanoidAppearanceComponent>(target))
        {
            var selfMessage = Loc.GetString("changeling-dna-fail-nohuman", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }

        if (!_mobState.IsIncapacitated(target)) // if target isn't crit or dead dont let absorb
        {
            var selfMessage = Loc.GetString("changeling-dna-fail-notdead", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }

        if (HasComp<AbsorbedComponent>(target))
        {
            var selfMessage = Loc.GetString("changeling-dna-alreadyabsorbed", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }

        args.Handled = true;

        _popup.PopupEntity(Loc.GetString("changeling-dna-stage-1"), uid, uid);

        var doAfter = new DoAfterArgs(EntityManager, uid, component.AbsorbDuration, new AbsorbDoAfterEvent(), uid, target: target)
        {
            DistanceThreshold = 2,
            BreakOnUserMove = true,
            BreakOnTargetMove = true,
            BreakOnDamage = true,
            AttemptFrequency = AttemptFrequency.StartAndEnd
        };

        _doAfter.TryStartDoAfter(doAfter);
    }

    public ProtoId<DamageGroupPrototype> GeneticDamageGroup = "Genetic";
    private void OnAbsorbDoAfter(EntityUid uid, ChangelingComponent component, AbsorbDoAfterEvent args)
    {
        if (args.Handled || args.Args.Target == null)
            return;

        args.Handled = true;
        args.Repeat = RepeatDoAfter(component);
        var target = args.Args.Target.Value;

        if (args.Cancelled || !_mobState.IsIncapacitated(target) || HasComp<AbsorbedComponent>(target))
        {
            var selfMessage = Loc.GetString("changeling-dna-interrupted", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessage, uid, uid);
            component.AbsorbStage = 0;
            args.Repeat = false;
            return;
        }

        if (component.AbsorbStage == 0)
        {
            var othersMessage = Loc.GetString("changeling-dna-stage-2-others", ("user", Identity.Entity(uid, EntityManager)));
            _popup.PopupEntity(othersMessage, uid, Filter.PvsExcept(uid), true, PopupType.MediumCaution);

            var selfMessage = Loc.GetString("changeling-dna-stage-2-self");
            _popup.PopupEntity(selfMessage, uid, uid, PopupType.MediumCaution);
        }
        else if (component.AbsorbStage == 1)
        {
            var othersMessage = Loc.GetString("changeling-dna-stage-3-others", ("user", Identity.Entity(uid, EntityManager)), ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(othersMessage, uid, Filter.PvsExcept(uid), true, PopupType.LargeCaution);

            var selfMessage = Loc.GetString("changeling-dna-stage-3-self", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessage, uid, uid, PopupType.LargeCaution);
        }
        else if (component.AbsorbStage == 2)
        {
            var doStealDNA = true;
            if (TryComp(target, out DnaComponent? dnaCompTarget))
            {
                foreach (var storedData in component.StoredDNA)
                {
                    if (storedData.DNA != null && storedData.DNA == dnaCompTarget.DNA)
                        doStealDNA = false;
                }
            }

            if (doStealDNA)
            {
                if (!StealDNA(uid, target, component))
                {
                    component.AbsorbStage = 0;
                    args.Repeat = false;
                    return;
                }
            }

            // give them 200 genetic damage and remove all of their blood
            var dmg = new DamageSpecifier(_proto.Index(GeneticDamageGroup), component.AbsorbGeneticDmg);
            _damageableSystem.TryChangeDamage(target, dmg);
            _bloodstreamSystem.ChangeBloodReagent(target, "FerrochromicAcid"); // replace target's blood with acid, then spill
            _bloodstreamSystem.SpillAllSolutions(target); // replace target's blood with acid, then spill
            EnsureComp<AbsorbedComponent>(target);

            if (HasComp<ChangelingComponent>(target)) // they were another changeling, give extra evolution points
            {
                var selfMessage = Loc.GetString("changeling-dna-success-ling", ("target", Identity.Entity(target, EntityManager)));
                _popup.PopupEntity(selfMessage, uid, uid, PopupType.Medium);

                if (TryComp<StoreComponent>(uid, out var store))
                {
                    _store.TryAddCurrency(new Dictionary<string, FixedPoint2> { { EvolutionPointsCurrencyPrototype, component.AbsorbedChangelingPointsAmount } }, uid, store);
                    _store.UpdateUserInterface(uid, uid, store);
                }
            }
            else
            {
                var selfMessage = Loc.GetString("changeling-dna-success", ("target", Identity.Entity(target, EntityManager)));
                _popup.PopupEntity(selfMessage, uid, uid, PopupType.Medium);
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
    private void OnRegenerate(EntityUid uid, ChangelingComponent component, LingRegenerateActionEvent args)
    {
        if (args.Handled)
            return;

        if (_mobState.IsDead(uid))
        {
            _popup.PopupEntity(Loc.GetString("changeling-regenerate-fail-dead"), uid, uid);
            return;
        }

        if (_mobState.IsCritical(uid)) // make sure the ling is critical, if not they cant regenerate
        {
            if (!TryUseAbility(uid, component, component.ChemicalsCostTen))
                return;

            args.Handled = true;

            var damage_brute = new DamageSpecifier(_proto.Index(BruteDamageGroup), component.RegenerateBruteHealAmount);
            var damage_burn = new DamageSpecifier(_proto.Index(BurnDamageGroup), component.RegenerateBurnHealAmount);
            _damageableSystem.TryChangeDamage(uid, damage_brute);
            _damageableSystem.TryChangeDamage(uid, damage_burn);
            _bloodstreamSystem.TryModifyBloodLevel(uid, component.RegenerateBloodVolumeHealAmount); // give back blood and remove bleeding
            _bloodstreamSystem.TryModifyBleedAmount(uid, component.RegenerateBleedReduceAmount);
            _audioSystem.PlayPvs(component.SoundRegenerate, uid);

            var othersMessage = Loc.GetString("changeling-regenerate-others-success", ("user", Identity.Entity(uid, EntityManager)));
            _popup.PopupEntity(othersMessage, uid, Filter.PvsExcept(uid), true, PopupType.MediumCaution);

            var selfMessage = Loc.GetString("changeling-regenerate-self-success");
            _popup.PopupEntity(selfMessage, uid, uid, PopupType.MediumCaution);
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("changeling-regenerate-fail-not-crit"), uid, uid);
        }
    }

    public const string ArmBladeId = "ArmBlade";
    private void OnArmBladeAction(EntityUid uid, ChangelingComponent component, ArmBladeActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp(uid, out HandsComponent? handsComponent))
            return;
        if (handsComponent.ActiveHand == null)
            return;

        var handContainer = handsComponent.ActiveHand.Container;

        if (handContainer == null)
            return;

        if (!TryUseAbility(uid, component, component.ChemicalsCostTwenty, !component.ArmBladeActive))
            return;

        args.Handled = true;

        if (!component.ArmBladeActive)
        {
            if (SpawnArmBlade(uid))
            {
                component.ArmBladeActive = true;
                _audioSystem.PlayPvs(component.SoundFlesh, uid);

                var othersMessage = Loc.GetString("changeling-armblade-success-others", ("user", Identity.Entity(uid, EntityManager)));
                _popup.PopupEntity(othersMessage, uid, Filter.PvsExcept(uid), true, PopupType.MediumCaution);

                var selfMessage = Loc.GetString("changeling-armblade-success-self");
                _popup.PopupEntity(selfMessage, uid, uid, PopupType.MediumCaution);
            }
            else
            {
                _popup.PopupEntity(Loc.GetString("changeling-armblade-fail"), uid, uid);
            }
        }
        else
        {
            if (handContainer.ContainedEntity != null)
            {
                if (TryComp<MetaDataComponent>(handContainer.ContainedEntity.Value, out var targetMeta))
                {
                    if (TryPrototype(handContainer.ContainedEntity.Value, out var prototype, targetMeta))
                    {
                        if (prototype.ID == ArmBladeId)
                        {
                            component.ArmBladeActive = false;
                            QueueDel(handContainer.ContainedEntity.Value);
                            _audioSystem.PlayPvs(component.SoundFlesh, uid);

                            var othersMessage = Loc.GetString("changeling-armblade-retract-others", ("user", Identity.Entity(uid, EntityManager)));
                            _popup.PopupEntity(othersMessage, uid, Filter.PvsExcept(uid), true, PopupType.MediumCaution);

                            var selfMessage = Loc.GetString("changeling-armblade-retract-self");
                            _popup.PopupEntity(selfMessage, uid, uid, PopupType.MediumCaution);
                        }
                    }
                }
            }
        }
    }

    public void SpawnLingArmor(EntityUid uid, InventoryComponent inventory)
    {
        var helmet = Spawn(LingHelmetId, Transform(uid).Coordinates);
        var armor = Spawn(LingArmorId, Transform(uid).Coordinates);
        EnsureComp<UnremoveableComponent>(helmet); // cant remove the armor
        EnsureComp<UnremoveableComponent>(armor); // cant remove the armor

        _inventorySystem.TryUnequip(uid, HeadId, true, true, false, inventory);
        _inventorySystem.TryEquip(uid, helmet, HeadId, true, true, false, inventory);
        _inventorySystem.TryUnequip(uid, OuterClothingId, true, true, false, inventory);
        _inventorySystem.TryEquip(uid, armor, OuterClothingId, true, true, false, inventory);
    }

    public bool SpawnArmBlade(EntityUid uid)
    {
        var armblade = Spawn(ArmBladeId, Transform(uid).Coordinates);
        EnsureComp<UnremoveableComponent>(armblade); // armblade is apart of your body.. cant remove it..

        if (_handsSystem.TryPickupAnyHand(uid, armblade))
        {
            return true;
        }
        else
        {
            QueueDel(armblade);
            return false;
        }
    }

    public const string LingHelmetId = "ClothingHeadHelmetLing";
    public const string LingArmorId = "ClothingOuterArmorChangeling";
    public const string HeadId = "head";
    public const string OuterClothingId = "outerClothing";

    private void OnLingArmorAction(EntityUid uid, ChangelingComponent component, LingArmorActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp(uid, out InventoryComponent? inventory))
            return;

        if (!TryUseAbility(uid, component, component.ChemicalsCostTwenty, !component.LingArmorActive, component.LingArmorRegenCost))
            return;

        _audioSystem.PlayPvs(component.SoundFlesh, uid);

        if (!component.LingArmorActive)
        {
            args.Handled = true;

            SpawnLingArmor(uid, inventory);

            var othersMessage = Loc.GetString("changeling-armor-success-others", ("user", Identity.Entity(uid, EntityManager)));
            _popup.PopupEntity(othersMessage, uid, Filter.PvsExcept(uid), true, PopupType.MediumCaution);

            var selfMessage = Loc.GetString("changeling-armor-success-self");
            _popup.PopupEntity(selfMessage, uid, uid, PopupType.MediumCaution);
        }
        else
        {
            if (_inventorySystem.TryGetSlotEntity(uid, HeadId, out var headitem) && _inventorySystem.TryGetSlotEntity(uid, OuterClothingId, out var outerclothingitem))
            {
                if (TryComp<MetaDataComponent>(headitem, out var targetHelmetMeta))
                {
                    if (TryPrototype(headitem.Value, out var prototype, targetHelmetMeta))
                    {
                        if (prototype.ID == LingHelmetId)
                        {
                            _inventorySystem.TryUnequip(uid, HeadId, true, true, false, inventory);
                        }
                    }
                }

                if (TryComp<MetaDataComponent>(outerclothingitem, out var targetArmorMeta))
                {
                    if (TryPrototype(outerclothingitem.Value, out var prototype, targetArmorMeta))
                    {
                        if (prototype.ID == LingArmorId)
                        {
                            _inventorySystem.TryUnequip(uid, OuterClothingId, true, true, false, inventory);
                        }
                    }
                }

                var othersMessage = Loc.GetString("changeling-armor-retract-others", ("user", Identity.Entity(uid, EntityManager)));
                _popup.PopupEntity(othersMessage, uid, Filter.PvsExcept(uid), true, PopupType.MediumCaution);

                var selfMessage = Loc.GetString("changeling-armor-retract-self");
                _popup.PopupEntity(selfMessage, uid, uid, PopupType.MediumCaution);

                var solution = new Solution();
                solution.AddReagent("Blood", FixedPoint2.New(75));
                _puddle.TrySpillAt(Transform(uid).Coordinates, solution, out _);
            }
        }

        component.LingArmorActive = !component.LingArmorActive;
    }

    private void OnLingInvisible(EntityUid uid, ChangelingComponent component, LingInvisibleActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp(uid, out InventoryComponent? inventory))
            return;

        if (!TryUseAbility(uid, component, component.ChemicalsCostTwentyFive, !component.ChameleonSkinActive))
            return;

        args.Handled = true;

        var stealth = EnsureComp<StealthComponent>(uid); // cant remove the armor
        var stealthonmove = EnsureComp<StealthOnMoveComponent>(uid); // cant remove the armor

        var message = Loc.GetString(!component.ChameleonSkinActive ? "changeling-chameleon-toggle-on" : "changeling-chameleon-toggle-off");
        _popup.PopupEntity(message, uid, uid);

        if (!component.ChameleonSkinActive)
        {
            stealthonmove.PassiveVisibilityRate = component.ChameleonSkinPassiveVisibilityRate;
            stealthonmove.MovementVisibilityRate = component.ChameleonSkinMovementVisibilityRate;
        }
        else
        {
            RemCompDeferred(uid, stealth);
            RemCompDeferred(uid, stealthonmove);
        }

        component.ChameleonSkinActive = !component.ChameleonSkinActive;
    }

    private void OnLingEmp(EntityUid uid, ChangelingComponent component, LingEMPActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryUseAbility(uid, component, component.ChemicalsCostTwenty))
            return;

        args.Handled = true;

        var coords = _transform.GetMapCoordinates(uid);
        _emp.EmpPulse(coords, component.DissonantShriekEmpRange, component.DissonantShriekEmpConsumption, component.DissonantShriekEmpDuration);
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
            if (storedData.DNA != null && storedData.DNA == dnaCompTarget.DNA)
            {
                var selfMessageFailAlreadyDna = Loc.GetString("changeling-dna-sting-fail-alreadydna", ("target", Identity.Entity(target, EntityManager)));
                _popup.PopupEntity(selfMessageFailAlreadyDna, uid, uid);
                return;
            }
        }

        if (!HasComp<HumanoidAppearanceComponent>(target))
        {
            var selfMessageFailNoHuman = Loc.GetString("changeling-dna-fail-nohuman", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessageFailNoHuman, uid, uid);
            return;
        }

        if (!TryUseAbility(uid, component, component.ChemicalsCostTwentyFive))
            return;

        if (StealDNA(uid, target, component))
        {
            args.Handled = true;

            var selfMessageSuccess = Loc.GetString("changeling-dna-sting", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessageSuccess, uid, uid);
        }
    }
}
