using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Client.Silicons.StationAi;
using Content.Shared._Starlight.OnCollide;
using Content.Shared._Starlight.Tag;
using Content.Shared.Inventory;
using Content.Shared.Silicons.StationAi;
using Content.Shared.Starlight.Medical.Surgery;
using Content.Shared.Tag;
using Content.Shared.Whitelist;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client._Starlight.Tag;
public sealed class StarlightTagSystem : StarlightSharedTagSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TagComponent, AfterAutoHandleStateEvent>(OnTagChanged);
    }

    private void OnTagChanged(Entity<TagComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if(ent == _player.LocalEntity)
        {
            var ev = new InvalidateLocalEntityTagEvent();
            RaiseLocalEvent(ref ev);
        }
    }
}

[ByRefEvent]
public record struct InvalidateLocalEntityTagEvent()
{
}
