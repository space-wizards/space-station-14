/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Content.Server.Zombies;
using Content.Shared._Offbrand.EntityEffects;
using Content.Shared.EntityEffects;

namespace Content.Server._Offbrand.EntityEffects;

public sealed class ZombifySystem : EntitySystem
{
    [Dependency] private readonly ZombieSystem _zombie = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExecuteEntityEffectEvent<Zombify>>(OnExecuteZombify);
    }

    private void OnExecuteZombify(ref ExecuteEntityEffectEvent<Zombify> args)
    {
        _zombie.ZombifyEntity(args.Args.TargetEntity);
    }
}
