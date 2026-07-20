using Content.Server.Speech.EntitySystems;
using Content.Shared.Speech.Components;

namespace Content.Server.Speech.Components;

[RegisterComponent]
[Access(typeof(SouthernAccentSystem))]
public sealed partial class SouthernAccentComponent : BaseAccentComponent;
