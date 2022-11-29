using Content.Shared.Ensnaring.Components;
using Robust.Shared.Containers;

namespace Content.Server.Ensnaring.Components;
[RegisterComponent]
[ComponentReference(typeof(SharedEnsnareableComponent))]
public sealed class EnsnareableComponent : SharedEnsnareableComponent
{
    /// <summary>
    /// The container where the <see cref="EnsnaringComponent"/> entity will be stored
    /// </summary>
    public Container Container = default!;
}
