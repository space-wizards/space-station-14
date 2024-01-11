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

namespace Content.Server.Changeling.EntitySystems;

public sealed partial class ChangelingSystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly HandsSystem _handsSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;

    private void InitializeLingAbilities()
    {
        SubscribeLocalEvent<ChangelingComponent, LingRegenerateActionEvent>(OnRegenerate);
        SubscribeLocalEvent<ChangelingComponent, ArmBladeActionEvent>(OnArmBladeAction);
        SubscribeLocalEvent<ChangelingComponent, LingArmorActionEvent>(OnLingArmorAction);
        SubscribeLocalEvent<ChangelingComponent, LingInvisibleActionEvent>(OnLingInvisible);
    }

    public ProtoId<DamageGroupPrototype> BruteDamageGroup = "Brute";
    public ProtoId<DamageGroupPrototype> BurnDamageGroup = "Burn";
    private void OnRegenerate(EntityUid uid, ChangelingComponent component, LingRegenerateActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (_mobState.IsDead(uid))
        {
            _popup.PopupEntity(Loc.GetString("changeling-regenerate-fail-dead"), uid, uid);
            return;
        }

        if (_mobState.IsCritical(uid)) // make sure the ling is critical, if not they cant regenerate
        {
            if (!TryUseAbility(uid, component, component.RegenerateChemicalsCost))
                return;
            var damage_brute = new DamageSpecifier(_proto.Index(BruteDamageGroup), -175f);
            var damage_burn = new DamageSpecifier(_proto.Index(BurnDamageGroup), -125f);
            _damageableSystem.TryChangeDamage(uid, damage_brute);
            _damageableSystem.TryChangeDamage(uid, damage_burn);
            _bloodstreamSystem.TryModifyBloodLevel(uid, 1000f); // give back blood and remove bleeding
            _bloodstreamSystem.TryModifyBleedAmount(uid, -1000f);
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

        args.Handled = true;

        if (!TryComp(uid, out HandsComponent? handsComponent))
            return;
        if (handsComponent.ActiveHand == null)
            return;

        var handContainer = handsComponent.ActiveHand.Container;

        if (handContainer == null)
            return;

        if (!TryUseAbility(uid, component, component.ArmBladeChemicalsCost, !component.ArmBladeActive))
            return;

        if (!component.ArmBladeActive)
        {
            var armblade = Spawn(ArmBladeId, Transform(uid).Coordinates);
            EnsureComp<UnremoveableComponent>(armblade); // armblade is apart of your body.. cant remove it..

            if (_handsSystem.TryGetEmptyHand(uid, out var emptyHand, handsComponent))
            {
                component.ArmBladeActive = true;
                _handsSystem.TryPickup(uid, armblade, emptyHand, false, false, handsComponent);
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

        args.Handled = true;

        if (!TryComp(uid, out InventoryComponent? inventory))
            return;

        if (!TryUseAbility(uid, component, component.LingArmorChemicalsCost, !component.LingArmorActive, component.LingArmorRegenCost))
            return;

        if (!component.LingArmorActive)
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
                        QueueDel(headitem.Value);
                    }
                }

                if (TryPrototype(outerclothingitem.Value, out var outerclothingproto))
                {
                    var result = _proto.HasIndex<EntityPrototype>(LingArmorId);
                    if (result)
                    {
                        _inventorySystem.TryUnequip(uid, OuterClothingId, true, true, false, inventory);
                        QueueDel(outerclothingitem.Value);
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

        args.Handled = true;

        if (!TryComp(uid, out InventoryComponent? inventory))
            return;

        if (!TryUseAbility(uid, component, component.ChameleonSkinChemicalsCost, !component.ChameleonSkinActive))
            return;

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
}
