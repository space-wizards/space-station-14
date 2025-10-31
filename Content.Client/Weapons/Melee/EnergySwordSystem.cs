using Content.Shared.Toggleable;
using Content.Shared.Weapons.Melee.EnergySword;
using Robust.Client.GameObjects;
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

    public void ChangeColorByClient(Entity<EnergySwordComponent> ent, Color color)
    {
        if (!TryComp(ent, out AppearanceComponent? appearanceComponent))
            return;

        ent.Comp.ActivatedColor = color;
        Dirty(ent);
        _appearance.SetData(ent, ToggleableVisuals.Color, ent.Comp.ActivatedColor, appearanceComponent);
    }
}
