
using Content.Shared.Popups;
using Content.Shared.Unitology.Components;
using Content.Shared.Stunnable;

namespace Content.Shared.Unitology;

public sealed class SharedUnitologySystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedStunSystem _sharedStun = default!;

    public override void Initialize()
    {
        base.Initialize();
    }
}
