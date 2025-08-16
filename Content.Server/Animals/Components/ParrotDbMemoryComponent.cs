using Content.Shared.Whitelist;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Animals.Components;

/// <summary>
/// Makes an entity have a persistent memory of messages for use with a ParrotMemoryComponent
/// </summary>
[RegisterComponent]
[AutoGenerateComponentPause]
public sealed partial class ParrotDbMemoryComponent : Component
{
    /// <summary>
    /// The next time at which this component will refresh the memory
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextRefresh = TimeSpan.Zero;
}
