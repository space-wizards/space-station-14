using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Hitscan.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HitscanIgniteComponent : Component
{
    //The chance the target has to ignite when hit by a hitscan.
    [DataField,AutoNetworkedField]
    public float IgniteChance = 0.25f;

    //How many fire stacks are added if ignition occurs.
    [DataField]
    public float FireStacks = 1f;
}
