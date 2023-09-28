using Robust.Shared.Network;

namespace Content.Shared.Damage.ForceSay;

/// <inheritdoc cref="DamageForceSayComponent"/>
public sealed class DamageForceSaySystem : EntitySystem
{
    [Dependency] private readonly INetManager _netManager = default!;

    public override void Initialize()
    {
        base.Initialize();


    }
}
