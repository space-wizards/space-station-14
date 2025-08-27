using Content.Shared._Ronstation.Vampire.Components;
using Content.Shared._Ronstation.Vampire.EntitySystems;
using Content.Shared.Antag;
using Content.Shared.StatusIcon.Components;
using Robust.Client.Player;
using Robust.Shared.Prototypes;

namespace Content.Client._Ronstation.Vampire.EntitySystems;

public sealed class VampireSystem : SharedVampireSystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();
    }
}