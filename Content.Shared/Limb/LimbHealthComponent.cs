using Robust.Shared.GameStates;

namespace Content.Shared.Limb;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LimbHealthComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<LimbSlot, int> Health = new()
    {
        { LimbSlot.Head, 100 },
        { LimbSlot.Arms, 100 },
        { LimbSlot.Torso, 100 },
        { LimbSlot.Groin, 100 },
        { LimbSlot.Legs, 100 }
    };
}
