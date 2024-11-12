using Content.Server.Atmos.Components;


namespace Content.Server.Atmos.EntitySystems;

/// <summary>
/// This handles...
/// </summary>
public sealed class EffectedByGasSystem : EntitySystem
{

    [Dependency] private readonly AtmosphereSystem _atmo = default!;
    public override void Initialize()
    {
        base.Initialize();

        //Subscribe to relevant events here.
        SubscribeLocalEvent<>();
    }
}
