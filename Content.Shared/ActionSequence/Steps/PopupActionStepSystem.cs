using Content.Shared.IdentityManagement;
using Content.Shared.Popups;

namespace Content.Shared.ActionSequence.Steps;

/// <summary>
/// System handling <see cref="PopupActionStep"/>.
/// </summary>
public sealed partial class PopupActionStepSystem : ActionStepSystem<PopupActionStep>
{
    [Dependency] private SharedPopupSystem _popup = default!;

    protected override void Step(Entity<ActionSequenceComponent> action, ref ActionStepEvent<PopupActionStep> args)
    {
        if (args.Step.Text == null)
            return;

        if (!SequenceSystem.TryGetBlackboardData<EntityUid>(action, args.Step.UserKey, out var user))
            return;

        var showLocation = user;
        if (SequenceSystem.TryGetBlackboardData<EntityUid>(action, args.Step.TargetKey, out var target) && !args.Step.ShowAtUser)
            showLocation = target;

        var text = Loc.GetString(args.Step.Text, ("user", Identity.Name(user, EntityManager)), ("target", Identity.Name(target, EntityManager)));

        _popup.PopupEntity(text, showLocation, user, args.Step.TextType);
        args.Handled = true;
    }
}

/// <summary>
/// Displays a popup to the UserKey at the entity location of LocationKey.
/// Has UserKey and TargetKey identities added to the GetString as user and target respectively.
/// </summary>
public sealed partial class PopupActionStep : ActionStepBase<PopupActionStep>
{
    /// <summary>
    /// LocId of the popup to show.
    /// </summary>
    [DataField]
    public LocId? Text;

    /// <summary>
    /// The popup type the popup should show as.
    /// </summary>
    [DataField]
    public PopupType TextType = PopupType.SmallCaution;

    /// <summary>
    /// Whether the popup should appear above the user.
    /// If False, it will appear above the Target if one exists.
    /// </summary>
    [DataField]
    public bool ShowAtUser = true;
}
