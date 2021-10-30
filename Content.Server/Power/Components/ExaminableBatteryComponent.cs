using Content.Shared.Examine;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.Power.Components
{
    [RegisterComponent]
#pragma warning disable 618
    public class ExaminableBatteryComponent : Component, IExamine
#pragma warning restore 618
    {
        public override string Name => "ExaminableBattery";

        [ViewVariables]
        [ComponentDependency] private BatteryComponent? _battery = default!;

        void IExamine.Examine(FormattedMessage message, bool inDetailsRange)
        {
            if (_battery == null)
                return;
            if (inDetailsRange)
            {
                var effectiveMax = _battery.MaxCharge;
                if (effectiveMax == 0)
                    effectiveMax = 1;
                var chargeFraction = _battery.CurrentCharge / effectiveMax;
                var chargePercentRounded = (int) (chargeFraction * 100);
                message.AddMarkup(
                    Loc.GetString(
                        "examinable-battery-component-examine-detail",
                        ("percent", chargePercentRounded),
                        ("markupPercentColor", "green")
                    )
                );
            }
        }
    }
}
