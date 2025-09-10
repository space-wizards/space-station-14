using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Server.Mapping;

[RegisterComponent]
public sealed partial class AutoSaveMapComponent : Component
{
    [DataField] public TimeSpan NextSaveTime;
    [DataField] public string FileName = string.Empty;
}
