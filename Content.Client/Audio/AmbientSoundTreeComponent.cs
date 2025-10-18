using Content.Shared.Audio;
using Robust.Shared.ComponentTrees;
using Robust.Shared.Physics;

namespace Content.Client.Audio;

/// <summary>
/// Samples nearby <see cref="AmbientSoundComponent"/> and plays audio.
/// </summary>
[RegisterComponent]
public sealed partial class AmbientSoundTreeComponent : Component, IComponentTreeComponent<AmbientSoundComponent>
{
    public DynamicTree<ComponentTreeEntry<AmbientSoundComponent>> Tree { get; set; } = default!;
}
