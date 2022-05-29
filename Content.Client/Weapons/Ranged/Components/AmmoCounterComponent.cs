using Content.Shared.Weapons.Ranged;
using Robust.Client.UserInterface;

namespace Content.Client.Weapons;

[RegisterComponent]
public sealed class AmmoCounterComponent : SharedAmmoCounterComponent
{
    public Control? Control;
}
