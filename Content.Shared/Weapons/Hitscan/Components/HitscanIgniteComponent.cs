using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Hitscan.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HitscanIgniteComponent : Component
{
    [DataField,AutoNetworkedField]
    public float IgniteChance = 0.25f;

    [DataField]
    public float FireStacks { get; set; }
}
