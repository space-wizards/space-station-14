using Content.Server.Speech.EntitySystems;
using Content.Shared.Whitelist;

namespace Content.Server.Speech.Components;

[RegisterComponent]
[Access(typeof(SpeechRequiresEquipmentSystem))]
public sealed partial class SpeechRequiresEquipmentComponent : Component
{
    [DataField(required: true)]
    public Dictionary<string, EntityWhitelist> Equipment;

    [DataField]
    public LocId? FailMessage;
}
