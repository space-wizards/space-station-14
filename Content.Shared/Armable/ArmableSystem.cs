using Content.Shared.Examine;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;

namespace Content.Shared.Armable;

/// <summary>
/// When used together with ItemToggle this will make the ItemToggle one way which is then used to represent an armed
/// state. If ItemComponent.Activated is true then the item is considered to be armed and should be able to be
/// triggered.
/// </summary>
public sealed class ArmableSystem : EntitySystem
{
    [Dependency] private readonly ItemToggleSystem _itemToggle = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArmableComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<ArmableComponent, ItemToggledEvent>(ArmingDone);
    }

    /// <summary>
    /// Shows the status of the armable entity on examination.
    /// </summary>
    private void OnExamine(EntityUid uid, ArmableComponent comp, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange || !comp.ShowStatusOnExamination || !TryComp<ItemToggleComponent>(uid, out var itemToggle))
            return;

        if (itemToggle.Activated)
        {
            if (!string.IsNullOrEmpty(comp.ExamineTextArmed))
                args.PushMarkup(Loc.GetString(comp.ExamineTextArmed, ("name", uid)));
        }
        else
        {
            if (!string.IsNullOrEmpty(comp.ExamineTextNotArmed))
                args.PushMarkup(Loc.GetString(comp.ExamineTextNotArmed,("name", uid)));
        }
    }

    /// <summary>
    /// Changes the appearance and disables the ItemToggleComponent as to not show the deactivate verb.
    /// Whatever is armed should probably not be trivially disarmed.
    /// </summary>
    private void ArmingDone(Entity<ArmableComponent> entity, ref ItemToggledEvent args)
    {
        if (!args.Activated)
            return;
        _itemToggle.SetOnActivate(entity.Owner, false);
    }
}
