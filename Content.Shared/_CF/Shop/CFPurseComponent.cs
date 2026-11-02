using Robust.Shared.GameStates;

namespace Content.Shared.CF.Shop;

/// <summary>Coins the character is carrying, stored as copper and shown as pp / gp / sp / cp.</summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CFPurseComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Copper;

    /// <summary>Set once at spawn so a later shop visit does not refill an empty purse.</summary>
    [DataField, AutoNetworkedField]
    public bool Seeded;
}
