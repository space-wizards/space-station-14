using Content.Shared.Cargo;

namespace Content.Client.Cargo;

public sealed partial class CargoSystem : SharedCargoSystem
{
    public override void Initialize()
    {
        base.Initialize();
        InitializeCargoTelepad();
    }
}
