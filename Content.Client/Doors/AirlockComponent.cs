using Content.Shared.Doors.Components;

namespace Content.Client.Doors;

[RegisterComponent]
[ComponentReference(typeof(SharedAirlockComponent))]
public sealed partial class AirlockComponent : SharedAirlockComponent { }
