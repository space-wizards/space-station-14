using Content.Shared.Examine;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
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
        [Dependency] private readonly IEntityManager _entityManager = default!;

        void IExamine.Examine(FormattedMessage message, bool inDetailsRange)
        {
            if (!_entityManager.TryGetComponent<BatteryComponent>(Owner, out var batteryComponent))
                return;
            if (inDetailsRange)
            {
                var effectiveMax = batteryComponent.MaxCharge;
                if (effectiveMax == 0)
                    effectiveMax = 1;
                var chargeFraction = batteryComponent.CurrentCharge / effectiveMax;
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
