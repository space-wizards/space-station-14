using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Toggleable;
using Content.Shared.Weapons.Melee.EnergySword;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Client.Weapons.Melee;
public sealed class EnergySwordSystem : SharedEnergySwordSystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly ItemToggleSystem _toggle = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    protected override void OpenInterface(Entity<EnergySwordComponent> ent, EntityUid actor)
    {
        if (!_ui.TryGetOpenUi(ent.Owner, EnergySwordColorUiKey.Key, out var bui))
        {
            _ui.TryOpenUi(ent.Owner, EnergySwordColorUiKey.Key, actor, predicted: true);
        }
    }

    public void ChangeColor(Entity<EnergySwordComponent> ent, Color color)
    {
        if (!TryComp(ent, out AppearanceComponent? appearanceComponent))
            return;

        ent.Comp.ActivatedColor = color;
        _appearance.SetData(ent, ToggleableVisuals.Color, ent.Comp.ActivatedColor, appearanceComponent);
    }

    public void ActivateSword(Entity<EnergySwordComponent> ent)
    {
        if (!TryComp(ent, out ItemToggleComponent? toggle))
            return;

        _toggle.TryActivate((ent.Owner, toggle), null, false);
    }
}
