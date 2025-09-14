/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

namespace Content.Shared._Offbrand.Wounds;

// smalldoggers guessed what these numbers meant just from the graphs on discord :]
[RegisterComponent]
public sealed partial class CryostasisFactorComponent : Component
{
    /// <summary>
    /// The body's temperature will be multiplied by this value to determine its contribution to the stasis factor
    /// </summary>
    [DataField(required: true)]
    public float TemperatureCoefficient;

    /// <summary>
    /// This constant will be added to the stasis factor
    /// </summary>
    [DataField(required: true)]
    public float TemperatureConstant;
}
