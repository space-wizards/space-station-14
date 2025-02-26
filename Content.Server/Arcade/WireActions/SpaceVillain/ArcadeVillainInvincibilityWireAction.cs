using Content.Server.Arcade.EntitySystems.SpaceVillain;
using Content.Server.Wires;
using Content.Shared.Arcade.SpaceVillain;
using Content.Shared.Wires;

namespace Content.Server.Arcade.WireActions.SpaceVillain;

/// <summary>
///
/// </summary>
public sealed partial class ArcadeVillainInvincibilityWireAction : BaseToggleWireAction
{
    private SpaceVillainArcadeSystem _villainArcadeSystem = default!;

    public override string Name { get; set; } = "wire-name-arcade-villain-invincible";
    public override Color Color { get; set; } = Color.Purple;
    public override object? StatusKey { get; } = VillainInvincibilityWireKey.StatusKey;

    public override void Initialize()
    {
        base.Initialize();

        _villainArcadeSystem = EntityManager.System<SpaceVillainArcadeSystem>();
    }

    public override void ToggleValue(EntityUid uid, bool setting)
    {
        _villainArcadeSystem.SetVillainInvincibility(uid, setting);
    }

    public override bool GetValue(EntityUid uid)
    {
        return _villainArcadeSystem.GetVillainInvincibility(uid);
    }

    public override StatusLightState? GetLightState(Wire wire)
    {
        return _villainArcadeSystem.GetVillainInvincibility(wire.Owner) ? StatusLightState.BlinkingSlow : StatusLightState.Off;
    }
}
