using Content.Shared.Mobs;

namespace Content.Client.Animals;

[RegisterComponent]
public sealed partial class ShearableVisualsComponent : Component
{
    [DataField("states")] public Dictionary<MobState, Dictionary<string, string>> States = new();

}







//AfterAutoHandleStateEvent