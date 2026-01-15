using Content.Shared.Destructible;
using Content.Shared.Destructible.Thresholds.Behaviors;
using Content.Shared.Popups;

namespace Content.Server.Destructible.Thresholds.Behaviors;

/// <summary>
/// Shows a popup for everyone.
/// </summary>
[DataDefinition]
public sealed partial class PopupBehavior : IThresholdBehavior
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    /// <summary>
    /// Locale id of the popup message.
    /// </summary>
    [DataField(required: true)]
    public string Popup;

    /// <summary>
    /// Type of popup to show.
    /// </summary>
    [DataField]
    public PopupType PopupType;

    /// <summary>
    /// Only the affected entity will see the popup.
    /// </summary>
    [DataField]
    public bool TargetOnly;

    public void Execute(EntityUid uid, SharedDestructibleSystem system, EntityUid? cause = null)
    {
        // Popup is placed at coords since the entity could be deleted after, no more popup then.
        var coords = system.EntityManager.GetComponent<TransformComponent>(uid).Coordinates;

        if (TargetOnly)
            _popup.PopupCoordinates(Loc.GetString(Popup), coords, uid, PopupType);
        else
            _popup.PopupCoordinates(Loc.GetString(Popup), coords, PopupType);
    }
}
