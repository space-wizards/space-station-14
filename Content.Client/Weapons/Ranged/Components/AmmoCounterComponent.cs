using Content.Shared.Weapons.Ranged.Components;
using Robust.Client.UserInterface;

namespace Content.Client.Weapons.Ranged.Components;

[RegisterComponent]
public sealed partial class AmmoCounterComponent : SharedAmmoCounterComponent
{
    public Control? Control;
}
