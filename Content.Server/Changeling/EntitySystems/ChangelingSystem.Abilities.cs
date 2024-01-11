using Content.Shared.Changeling.Components;
using Content.Shared.Changeling;
using Content.Shared.Inventory;
using Content.Shared.Interaction.Components;
using Content.Shared.Hands.Components;
using Content.Server.Hands.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server.Changeling.EntitySystems;

public sealed partial class ChangelingSystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly HandsSystem _handsSystem = default!;

    private void InitializeLingAbilities()
    {
        SubscribeLocalEvent<ChangelingComponent, ArmBladeActionEvent>(OnArmBladeAction);
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

        if (!component.ArmBladeActive)
        {
            if (!TryUseAbility(uid, component, component.ArmBladeChemicalsCost))
                return;

            var armblade = Spawn(ArmBladeId, Transform(uid).Coordinates);
            EnsureComp<UnremoveableComponent>(armblade); // armblade is apart of your body.. cant remove it..

            if (_handsSystem.TryGetEmptyHand(uid, out var emptyHand, handsComponent))
            {
                _handsSystem.TryPickup(uid, armblade, emptyHand, false, false, handsComponent);
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
                        QueueDel(handContainer.ContainedEntity.Value);
                        component.ArmBladeActive = false;
                    }
                }
            }
        }
    }
}
