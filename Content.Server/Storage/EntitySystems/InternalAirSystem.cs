using System.Linq;
using Content.Server.Storage.Components;
using Content.Shared.Database;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Storage;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Random;

namespace Content.Server.Storage.EntitySystems;

// TODO: move this to shared for verb prediction if/when storage is in shared
public sealed class InternalAirSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InternalAirComponent, ComponentInit>(OnInit);
    }
    private void OnInit(EntityUid uid, InternalAirComponent comp, ComponentInit args)
    {
        comp.Air.Volume = comp.Volume;
    }
}
