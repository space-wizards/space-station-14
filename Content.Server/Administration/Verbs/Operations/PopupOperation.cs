using Content.Shared.Popups;
using Robust.Shared.Player;

namespace Content.Server.Administration.Verbs.Operations;

public sealed partial class AdminOperationSystem
{
    [SubscribeLocalEvent]
    private void OnPopup(Entity<MetaDataComponent> entity, ref AdminOperationEvent<PopupOperation> args)
    {
        var message = Loc.GetString(args.Operation.Message,
            ("name", entity.Owner),
            ("entity", entity.Owner));

        switch (args.Operation.Recipients, args.Operation.Location)
        {
            case (PopupRecipients.Target, PopupLocation.Entity):
                _popup.PopupEntity(message, entity, entity, args.Operation.Type);
                break;
            case (PopupRecipients.Target, PopupLocation.Coordinates):
                _popup.PopupCoordinates(message, Transform(entity).Coordinates, entity, args.Operation.Type);
                break;
            case (PopupRecipients.Pvs, PopupLocation.Entity):
                _popup.PopupEntity(message, entity, args.Operation.Type);
                break;
            case (PopupRecipients.Pvs, PopupLocation.Coordinates):
                _popup.PopupCoordinates(message, Transform(entity).Coordinates, args.Operation.Type);
                break;
            case (PopupRecipients.PvsExceptTarget, PopupLocation.Entity):
                _popup.PopupEntity(message, entity, Filter.PvsExcept(entity), true, args.Operation.Type);
                break;
            case (PopupRecipients.PvsExceptTarget, PopupLocation.Coordinates):
                _popup.PopupCoordinates(
                    message,
                    Transform(entity).Coordinates,
                    Filter.PvsExcept(entity),
                    true,
                    args.Operation.Type);
                break;
        }
    }
}

public sealed partial class PopupOperation : AdminOperationBase<PopupOperation>
{
    /// <summary>
    /// The target is passed to localization as both <c>$name</c> and <c>$entity</c>.
    /// </summary>
    [DataField(required: true)]
    public LocId Message { get; private set; }

    [DataField]
    public PopupRecipients Recipients { get; private set; } = PopupRecipients.Target;

    [DataField]
    public PopupLocation Location { get; private set; } = PopupLocation.Entity;

    [DataField]
    public PopupType Type { get; private set; } = PopupType.Small;
}

public enum PopupRecipients : byte
{
    Target,
    Pvs,
    PvsExceptTarget
}

public enum PopupLocation : byte
{
    Entity,
    Coordinates
}
