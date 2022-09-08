using System.Globalization;
using Content.Server.EUI;
using Content.Shared.Eui;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Prototypes;

namespace Content.Server.Weapons.Melee;

public sealed class MeleeWeaponEui : BaseEui
{
    public override EuiStateBase GetNewState()
    {
        var values = new List<string[]>();
        var compFactory = IoCManager.Resolve<IComponentFactory>();
        var protoManager = IoCManager.Resolve<IPrototypeManager>();

        foreach (var proto in protoManager.EnumeratePrototypes<EntityPrototype>())
        {
            if (proto.Abstract ||
                !proto.Components.TryGetValue(compFactory.GetComponentName(typeof(MeleeWeaponComponent)),
                    out var meleeComp))
            {
                continue;
            }

            var comp = (MeleeWeaponComponent) meleeComp.Component;

            values.Add(new[]
            {
                proto.ID,
                (comp.Damage.Total * comp.AttackRate).ToString(),
                comp.AttackRate.ToString(CultureInfo.CurrentCulture),
                comp.Damage.Total.ToString(),
                comp.Range.ToString(CultureInfo.CurrentCulture),
            });
        }

        var state = new MeleeValuesEuiState()
        {
            Headers = new List<string>()
            {
                "ID",
                "DPS",
                "Attack Rate",
                "Damage",
                "Range",
            },
            Values = values,
        };

        return state;
    }
}
