using Content.Shared.Cargo;
using Robust.Client.GameObjects;

namespace Content.Client.Cargo.Systems;

[InjectDependencies]
public sealed partial class CargoSystem : SharedCargoSystem
{
    [Dependency] private AnimationPlayerSystem _player = default!;

    public override void Initialize()
    {
        base.Initialize();
        InitializeCargoTelepad();
    }
}
