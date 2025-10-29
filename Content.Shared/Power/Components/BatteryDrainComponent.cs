using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared.Power.Components;

/// <summary>
/// Drains battery passively when the entity used ItemToggle and left it on
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class BatteryDrainComponent : Component
{
    /// <summary>
    /// Amount of wattage/power that is drained by second
    /// </summary>
    [DataField]
    public float DrainAmount;

    /// <summary>
    /// When the next automatic power draw will occur
    /// </summary>
    [DataField, AutoPausedField, AutoNetworkedField]
    public TimeSpan NextUpdateTime;

    /// <summary>
    /// How long to wait between power drawing.
    /// </summary>
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(1);
}
