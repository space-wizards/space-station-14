/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Content.Shared.EntityEffects;
using Content.Shared.Zombies;
using Robust.Shared.Prototypes;

namespace Content.Shared._Offbrand.EntityEffects;

public sealed partial class IsZombieImmune : EntityEffectCondition
{
    [DataField]
    public bool Invert = false;

    public override bool Condition(EntityEffectBaseArgs args)
    {
        return args.EntityManager.HasComponent<ZombieImmuneComponent>(args.TargetEntity) ^ Invert;
    }

    public override string GuidebookExplanation(IPrototypeManager prototype)
    {
        return Loc.GetString("reagent-effect-condition-guidebook-is-zombie-immune", ("invert", Invert));
    }
}
