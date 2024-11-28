using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Shared.Changeling.Devour;
using Content.Shared.Devour.Components;
using Content.Shared.Devour;
using Content.Shared.DoAfter;
using Content.Server.DoAfter;
using Content.Shared.Damage;
using Content.Shared.Actions;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Server.Changeling.Devour;

public sealed class ChangelingDevourSystem : SharedChangelingDevourSystem
{
    public override void Initialize()
    {
        base.Initialize();
    }
}
