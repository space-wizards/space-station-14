using Content.Client.Light;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Light;
using Content.Shared.Light.Components;
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
    [Dependency] private readonly RgbLightControllerSystem _rgbSystem = default!;

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
        if (!TryComp(ent, out AppearanceComponent? appearance))
            return;

        ent.Comp.ActivatedColor = color;
        _appearance.SetData(ent, ToggleableVisuals.Color, ent.Comp.ActivatedColor, appearance);
    }

    public void ActivateSword(Entity<EnergySwordComponent> ent)
    {
        if (!TryComp(ent, out ItemToggleComponent? toggle) || !TryComp(ent, out AppearanceComponent? appearance))
            return;
        _toggle.TryActivate((ent.Owner, toggle), null, false);
        _appearance.SetData(ent, ToggleableVisuals.Enabled, true, appearance);
    }

    public void ActivateRGB(Entity<EnergySwordComponent> ent)
    {
        if (!TryComp(ent, out AppearanceComponent? appearance))
            return;

        var rgb = EnsureComp<RgbLightControllerComponent>(ent);
        _rgbSystem.SetCycleRate(ent, ent.Comp.CycleRate, rgb);
        _appearance.SetData(ent, ToggleableVisuals.Color, true, appearance);

    }
}
