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

    private void InitializeLingAbilities()
    {
        SubscribeLocalEvent<ChangelingComponent, LingAbsorbActionEvent>(StartAbsorbing);
        SubscribeLocalEvent<ChangelingComponent, AbsorbDoAfterEvent>(OnAbsorbDoAfter);

        SubscribeLocalEvent<ChangelingComponent, LingRegenerateActionEvent>(OnRegenerate);
        SubscribeLocalEvent<ChangelingComponent, ArmBladeActionEvent>(OnArmBladeAction);
        SubscribeLocalEvent<ChangelingComponent, LingArmorActionEvent>(OnLingArmorAction);
        SubscribeLocalEvent<ChangelingComponent, LingInvisibleActionEvent>(OnLingInvisible);
        SubscribeLocalEvent<ChangelingComponent, LingEMPActionEvent>(OnLingEmp);
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
            component.AbsorbStage = 0.0f;
            args.Repeat = false;
            return;
        }

        if (component.AbsorbStage == 0.0)
        {
            var othersMessage = Loc.GetString("changeling-dna-stage-2-others", ("user", Identity.Entity(uid, EntityManager)));
            _popup.PopupEntity(othersMessage, uid, Filter.PvsExcept(uid), true, PopupType.MediumCaution);

            var selfMessage = Loc.GetString("changeling-dna-stage-2-self");
            _popup.PopupEntity(selfMessage, uid, uid, PopupType.MediumCaution);
        }
        else if (component.AbsorbStage == 1.0)
        {
            var othersMessage = Loc.GetString("changeling-dna-stage-3-others", ("user", Identity.Entity(uid, EntityManager)), ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(othersMessage, uid, Filter.PvsExcept(uid), true, PopupType.LargeCaution);

            var selfMessage = Loc.GetString("changeling-dna-stage-3-self", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessage, uid, uid, PopupType.LargeCaution);
        }
        else if (component.AbsorbStage == 2.0)
        {
            if (!StealDNA(uid, target, component))
            {
                component.AbsorbStage = 0.0f;
                args.Repeat = false;
                return;
            }

            var selfMessage = Loc.GetString("changeling-dna-success", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessage, uid, uid, PopupType.Medium);

            // give them 200 genetic damage and remove all of their blood
            var dmg = new DamageSpecifier(_proto.Index(GeneticDamageGroup), component.AbsorbGeneticDmg);
            _damageableSystem.TryChangeDamage(target, dmg);
            _bloodstreamSystem.ChangeBloodReagent(target, "FerrochromicAcid"); // replace target's blood with acid, then spill
            _bloodstreamSystem.SpillAllSolutions(target); // replace target's blood with acid, then spill
            EnsureComp<AbsorbedComponent>(target);
        }

        if (component.AbsorbStage >= 2.0)
            component.AbsorbStage = 0.0f;
        else
            component.AbsorbStage += 1.0f;
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
            if (!TryUseAbility(uid, component, component.RegenerateChemicalsCost))
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

        if (!TryUseAbility(uid, component, component.ArmBladeChemicalsCost, !component.ArmBladeActive))
            return;

        args.Handled = true;

        if (!component.ArmBladeActive)
        {
            var armblade = Spawn(ArmBladeId, Transform(uid).Coordinates);
            var unremoveableComp = EnsureComp<UnremoveableComponent>(armblade); // armblade is apart of your body.. cant remove it..
            unremoveableComp.DeleteOnDrop = false;

            if (_handsSystem.TryPickupAnyHand(uid, armblade))
            {
                component.ArmBladeActive = true;
            }
            else
            {
                _popup.PopupEntity(Loc.GetString("changeling-armblade-fail"), uid, uid);
                QueueDel(armblade);
            }
        }
        else
        {
            if (handContainer.ContainedEntity != null)
            {
                if (TryPrototype(handContainer.ContainedEntity.Value, out var protoInHand))
                {
                    var result = _proto.HasIndex<EntityPrototype>(ArmBladeId);
                    if (result)
                    {
                        component.ArmBladeActive = false;
                        QueueDel(handContainer.ContainedEntity.Value);
                    }
                }
            }
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

        if (!TryUseAbility(uid, component, component.LingArmorChemicalsCost, !component.LingArmorActive, component.LingArmorRegenCost))
            return;

        args.Handled = true;

        if (!component.LingArmorActive)
        {
            var helmet = Spawn(LingHelmetId, Transform(uid).Coordinates);
            var armor = Spawn(LingArmorId, Transform(uid).Coordinates);
            var compHelmet = EnsureComp<UnremoveableComponent>(helmet); // cant remove the armor
            var compArmor = EnsureComp<UnremoveableComponent>(armor); // cant remove the armor
            compHelmet.DeleteOnDrop = false;
            compArmor.DeleteOnDrop = false;

            _inventorySystem.TryUnequip(uid, HeadId, true, true, false, inventory);
            _inventorySystem.TryEquip(uid, helmet, HeadId, true, true, false, inventory);
            _inventorySystem.TryUnequip(uid, OuterClothingId, true, true, false, inventory);
            _inventorySystem.TryEquip(uid, armor, OuterClothingId, true, true, false, inventory);
        }
        else
        {
            if (_inventorySystem.TryGetSlotEntity(uid, HeadId, out var headitem) && _inventorySystem.TryGetSlotEntity(uid, OuterClothingId, out var outerclothingitem))
            {
                if (TryPrototype(headitem.Value, out var headitemproto))
                {
                    var result = _proto.HasIndex<EntityPrototype>(LingHelmetId);
                    if (result)
                    {
                        _inventorySystem.TryUnequip(uid, HeadId, true, true, false, inventory);
                        QueueDel(headitem);
                    }
                }

                if (TryPrototype(outerclothingitem.Value, out var outerclothingproto))
                {
                    var result = _proto.HasIndex<EntityPrototype>(LingArmorId);
                    if (result)
                    {
                        _inventorySystem.TryUnequip(uid, OuterClothingId, true, true, false, inventory);
                        QueueDel(outerclothingitem);
                    }
                }
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

        if (!TryUseAbility(uid, component, component.ChameleonSkinChemicalsCost, !component.ChameleonSkinActive))
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

        if (!TryUseAbility(uid, component, component.DissonantShriekChemicalsCost))
            return;

        args.Handled = true;

        var coords = _transform.GetMapCoordinates(uid);
        _emp.EmpPulse(coords, component.DissonantShriekEmpRange, component.DissonantShriekEmpConsumption, component.DissonantShriekEmpDuration);
    }
}
