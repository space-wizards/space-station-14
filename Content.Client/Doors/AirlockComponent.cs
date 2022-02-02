using Content.Shared.Doors.Components;
using Robust.Shared.GameObjects;

namespace Content.Client.Doors;

[RegisterComponent]
[ComponentReference(typeof(SharedAirlockComponent))]
public sealed class AirlockComponent : SharedAirlockComponent { }
