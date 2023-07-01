using Content.Shared.Access.Components;
using Content.Shared.Containers.ItemSlots;
using JetBrains.Annotations;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Doors.Electronics;

[UsedImplicitly]
public abstract class SharedDoorElectronicsSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private readonly ILogManager _log = default!;

    public const string Sawmill = "doorelectronics";
    protected ISawmill _sawmill = default!;
}
