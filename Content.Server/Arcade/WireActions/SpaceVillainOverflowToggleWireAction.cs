using Content.Server.Wires;
using Content.Shared.Arcade.Components;
using Content.Shared.Arcade.Enums;
using Content.Shared.Wires;

namespace Content.Server.Arcade.WireActions;

public sealed partial class SpaceVillainOverflowToggleWireAction : BaseToggleWireAction
{
    public override string Name { get; set; } = "wire-name-space-villain-overflow";
    public override Color Color { get; set; } = Color.AliceBlue;
    public override object StatusKey { get; } = SpaceVillainArcadeWireStatus.Overflow;

    public override StatusLightState? GetLightState(Wire wire)
    {
        return GetValue(wire.Owner) ? StatusLightState.BlinkingSlow : StatusLightState.Off;
    }

    public override void ToggleValue(EntityUid owner, bool setting)
    {
        if (setting)
            EntityManager.EnsureComponent<SpaceVillainArcadeOverflowComponent>(owner);
        else
            EntityManager.RemoveComponent<SpaceVillainArcadeOverflowComponent>(owner);
    }

    public override bool GetValue(EntityUid owner)
    {
        return EntityManager.HasComponent<SpaceVillainArcadeOverflowComponent>(owner);
    }
}
