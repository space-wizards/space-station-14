using Robust.Shared.GameStates;

namespace Content.Shared.Lens;

/// <summary>
///     This component is used alongside <see cref="ItemSlotsComponent"/> to find a specific container.
///     Enables clothing to proxy the functionality of an item inside of it.
/// </summary>
[RegisterComponent]
public sealed partial class LensSlotComponent : Component
{
    [DataField("LensSlotId", required: true)]
    public string LensSlotId = string.Empty;
}

/// <summary>
///     Raised directed at an entity with <see cref="LensSlotComponent"/> when the lens is ejected/inserted.
/// </summary>
public sealed class LensChangedEvent : EntityEventArgs
{
    public readonly bool Ejected;
    public readonly EntityUid Lens;

    public LensChangedEvent(EntityUid lens, bool ejected)
    {
        Lens = lens;
        Ejected = ejected;
    }
}
