/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;

namespace Content.Shared._Offbrand.Wounds;

public static class SolutionExtensions
{
    public static bool HasOverlapAtLeast(this Solution solution, Solution incoming, FixedPoint2 threshold)
    {
        var count = FixedPoint2.Zero;

        foreach (var (reagent, quantity) in incoming.Contents)
        {
            count += solution.GetReagentQuantity(reagent);
        }

        return count >= threshold;
    }
}
