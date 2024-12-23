using Content.Shared.Examine;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Toggleable;


namespace Content.Shared.Armable;

public sealed class ArmableSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArmableComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<ArmableComponent, ItemToggleActivateAttemptEvent>(TryArming);
        SubscribeLocalEvent<ArmableComponent, ItemToggledEvent>(ArmingDone);
    }

    private void TryArming(Entity<ArmableComponent> entity, ref ItemToggleActivateAttemptEvent args)
    {
        if (TryComp<ItemToggleComponent>(entity, out var comp))
            comp.Activated = true;
    }

    private void OnExamine(EntityUid uid, ArmableComponent comp, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange || !comp.ShowStatusOnExamination || !TryComp<ItemToggleComponent>(uid, out var itemToggle))
            return;

        if(itemToggle.Activated)
            args.PushMarkup(Loc.GetString("examine-armed", ("name", uid)));
        else
            args.PushMarkup(Loc.GetString("examine-not-armed", ("name", uid)));
    }

    /// <summary>
    /// Changes the appearance and disables the ItemToggleComponent.
    /// Whatever is armed should probably not be trivially disarmed.
    /// </summary>
    private void ArmingDone(Entity<ArmableComponent> entity, ref ItemToggledEvent args)
    {
        if (!TryComp<ItemToggleComponent>(entity, out var comp))
            return;

        if (TryComp<AppearanceComponent>(entity, out var appearance))
            _appearance.SetData(entity, ToggleVisuals.Toggled, comp.Activated, appearance);

        comp.OnActivate = false;
    }
}
