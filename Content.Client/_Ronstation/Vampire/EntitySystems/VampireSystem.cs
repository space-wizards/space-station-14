using Content.Shared._Ronstation.Vampire.Components;
using Content.Shared._Ronstation.Vampire.EntitySystems;
using Content.Shared.Antag;
using Content.Shared.StatusIcon.Components;
using Robust.Client.Player;
using Robust.Shared.Prototypes;

namespace Content.Client._Ronstation.BloodBrothers.EntitySystems;

public sealed class VampireSystem : SharedVampireSystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VampireComponent, GetStatusIconsEvent>(OnVampireGetIcons);
    }
    private void OnVampireGetIcons(Entity<VampireComponent> entity, ref GetStatusIconsEvent args)
    {
        if (_playerManager.LocalSession?.AttachedEntity is { } playerEntity)
        {
            if (!HasComp<ShowAntagIconsComponent>(playerEntity) &&
                entity.Owner != playerEntity)
                return;
        }

        // if (_prototypeManager.TryIndex(entity.Comp.VampireIcon, out var iconPrototype))
        //     args.StatusIcons.Add(iconPrototype);
    }
}