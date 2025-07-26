using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Mobs;
using Content.Shared.Silicons.Borgs;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Server.GameObjects;

namespace Content.Server.Silicons.Borgs;

public sealed class BorgDisguiseSystem : SharedBorgDisguiseSystem
{
    [Dependency] private readonly SharedPointLightSystem _pointLightSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BorgDisguiseComponent, BorgDisguiseToggleActionEvent>(OnDisguiseToggle);
        SubscribeLocalEvent<BorgDisguiseComponent, ItemToggledEvent>(OnToggled);
        SubscribeLocalEvent<BorgDisguiseComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    /// <summary>
    /// Toggles the disguise.
    /// </summary>
    /// <param name="uid">The entity to toggle the disguise of.</param>
    /// <param name="comp">The disguise component of the entity.</param>
    /// <param name="args"></param>
    private void OnDisguiseToggle(EntityUid uid, BorgDisguiseComponent comp, BorgDisguiseToggleActionEvent args)
    {
        if (args.Handled)
            return;
        comp.Disguised = !comp.Disguised;
        Dirty(uid, comp);
        args.Handled = true;
        UpdateApperance(uid, comp);
    }

    /// <summary>
    /// Disables the disguise.
    /// </summary>
    /// <param name="uid">The entity having their disguise disabled.</param>
    /// <param name="comp">The disguise component being disabled.</param>
    private void DisableDisguise(EntityUid uid, BorgDisguiseComponent comp)
    {
        comp.Disguised = false;
        Dirty(uid, comp);
        UpdateApperance(uid, comp);
    }

    /// <summary>
    /// Disables the disguise if the borg is no longer powered.
    /// </summary>
    /// <param name="uid">The entity to check</param>
    /// <param name="comp">The disguise component.</param>
    /// <param name="args">State change event.</param>
    private void OnToggled(EntityUid uid, BorgDisguiseComponent comp, ref ItemToggledEvent args)
    {
        if (!args.Activated)
        {
            DisableDisguise(uid, comp);
        }
    }

    /// <summary>
    /// Disables the disguise if the borg is no longer alive.
    /// </summary>
    /// <param name="uid">The entity to check</param>
    /// <param name="component">The disguise component.</param>
    /// <param name="args">State change event.</param>
    private void OnMobStateChanged(EntityUid uid, BorgDisguiseComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Alive)
        {
            DisableDisguise(uid, component);
        }
    }

    /// <summary>
    /// Updates the appearance data of the entity.
    /// </summary>
    /// <param name="uid">The entity to update.</param>
    /// <param name="comp">The component holding the disguise data.</param>
    private void UpdateApperance(EntityUid uid, BorgDisguiseComponent comp)
    {
        if (TryPrototype(uid, out var entityPrototype))
        {
            if (entityPrototype.TryGetComponent<PointLightComponent>("PointLight", out var lightPrototype))
            {
                _pointLightSystem.SetColor(uid,
                    comp.Disguised
                        ? comp.DisguisedLightColor
                        : lightPrototype.Color);
            }
        }

        UpdateSharedAppearance(uid, comp);
    }
}
