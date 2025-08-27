using Content.Shared.Popups;

namespace Content.Server.Destructible.Thresholds.Behaviors;

/// <summary>
/// Shows a popup for everyone.
/// </summary>
[DataDefinition]
public sealed partial class PopupBehavior : IThresholdBehavior
{
    /// <summary>
    /// Locale id of the popup message.
    /// </summary>
    [DataField("popup", required: true)]
    public string Popup;

    /// <summary>
    /// Type of popup to show.
    /// </summary>
    [DataField("popupType")]
    public PopupType PopupType;

    /// <summary>
    /// Only the affected entity will see the popup.
    /// </summary>
    [DataField]
    public bool TargetOnly;

    public void Execute(EntityUid uid, DestructibleSystem system, EntityUid? cause = null)
    {
        var popup = system.EntityManager.System<SharedPopupSystem>();
        // popup is placed at coords since the entity could be deleted after, no more popup then
        var coords = system.EntityManager.GetComponent<TransformComponent>(uid).Coordinates;

        if (TargetOnly)
            popup.PopupCoordinates(Loc.GetString(Popup), coords, uid, PopupType);
        else
            popup.PopupCoordinates(Loc.GetString(Popup), coords, PopupType);
    }
}
