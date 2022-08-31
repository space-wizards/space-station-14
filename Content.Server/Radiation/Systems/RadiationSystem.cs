using Content.Shared.Radiation.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Map;

namespace Content.Server.Radiation.Systems;

public sealed partial class RadiationSystem : SharedRadiationSystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly SharedPhysicsSystem _physicsSystem = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private const float RadiationCooldown = 1.0f;
    private float _accumulator;

    public override void Initialize()
    {
        base.Initialize();
        InitRadBlocking();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        _accumulator += frameTime;

        while (_accumulator > RadiationCooldown)
        {
            _accumulator -= RadiationCooldown;

            UpdateGridcast();
        }
    }
}
