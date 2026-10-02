using Robust.Shared.GameStates;

namespace Content.Shared.Humanoid;

[RegisterComponent, NetworkedComponent]
public sealed partial class RandomHumanoidAppearanceComponent : Component
{
    [DataField]
    public bool RandomizeName = true;
}
