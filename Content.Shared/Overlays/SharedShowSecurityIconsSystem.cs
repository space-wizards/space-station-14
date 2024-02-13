using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;

namespace Content.Shared.Overlays;

public abstract class SharedShowSecurityIconsSystem : EntitySystem
{
}

/// <summary>
///     Gets called whenever a security hud is requesting icons from an entity.
/// </summary>
[ByRefEvent]
public record struct GetCriminalIconEvent(bool Cancelled = false, ProtoId<StatusIconPrototype>? Icon = null);
