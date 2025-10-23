using Robust.Shared.GameStates;

namespace Content.Shared.UserInterface;

[RegisterComponent, NetworkedComponent]
public sealed partial class MultiActivatableUIComponent : Component
{
    [DataField]
    public List<Enum> Keys = [];

    [DataField]
    public List<LocId> VerbTexts = [];
}
