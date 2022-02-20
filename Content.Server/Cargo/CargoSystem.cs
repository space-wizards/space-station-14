using Content.Shared.Cargo;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Server.Cargo;

public sealed partial class CargoSystem : SharedCargoSystem
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly ItemSlotsSystem _slots = default!;

    public override void Initialize()
    {
        base.Initialize();
        InitializeConsole();
        InitializeTelepad();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        UpdateConsole(frameTime);
        UpdateTelepad(frameTime);
    }
}
