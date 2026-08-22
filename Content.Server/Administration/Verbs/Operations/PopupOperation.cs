using Content.Shared.Popups;

namespace Content.Server.Administration.Verbs.Operations;

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
