using Content.Shared.Mobs;
using Robust.Shared.Serialization;

namespace Content.Shared.Animals;

[Access(typeof(SharedShearableSystem))]
[RegisterComponent]
public sealed partial class ShearableVisualsComponent : Component
{

    [ViewVariables]
    public bool Sheared { get; set; } = false;

    // MobState is one of the four mobstates, e.g. alive, dead, invalid, critical
    // the string is the sprite state for that mobstate.
    [ViewVariables]
    public Dictionary<MobState, string> States { get; set; } = [];

    [Serializable, NetSerializable]
    public enum ShearableVisuals
    {
        Sheared,
        States,
    }

}







//AfterAutoHandleStateEvent