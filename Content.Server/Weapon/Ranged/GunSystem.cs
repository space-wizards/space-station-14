using Content.Shared.Popups;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Weapon.Ranged;

public sealed partial class GunSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
}
