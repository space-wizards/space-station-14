using Content.Shared.Alert;
using Robust.Client.GameObjects;

namespace Content.Client.Alerts;

/// <summary>
/// Event raised on an entity with alerts in order to allow it to update visuals for the alert sprite entity.
/// </summary>
[ByRefEvent]
public record struct UpdateAlertSpriteEvent
{
    public Entity<SpriteComponent> SpriteViewEnt;

    public EntityUid ViewerEnt;

    public AlertPrototype Alert;

    public UpdateAlertSpriteEvent(Entity<SpriteComponent> spriteViewEnt, EntityUid viewerEnt, AlertPrototype alert)
    {
        SpriteViewEnt = spriteViewEnt;
        ViewerEnt = viewerEnt;
        Alert = alert;
    }
}
