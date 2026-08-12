using Content.Shared.EntityEffects;
using Content.Shared.Popups;

namespace Content.Shared.ActionSequence;

/// <summary>
/// This handles entity effects.
/// Specifically it handles the receiving of events for causing entity effects, and provides
/// public API for other systems to take advantage of entity effects.
/// </summary>
public sealed partial class PopupActionStepSystem : ActionStepSystem<PopupActionStep>
{
    [Dependency] private SharedPopupSystem _popup = default!;

    protected override void Effect(Entity<ActionSequenceComponent> entity, ref ActionStepEvent<PopupActionStep> args)
    {
        Log.Debug("Popup detected");
        if (!entity.Comp.Blackboard.TryGetValue(args.Effect.UserKey, out var userKey) || userKey is not EntityUid user)
            return;

        Log.Debug("Displaying popup");

        _popup.PopupEntity(Loc.GetString(args.Effect.Text), user, user, PopupType.SmallCaution);
        args.Handled = true;
    }
}

/// <summary>
/// See serverside system.
/// </summary>
/// <inheritdoc cref="EntityEffect"/>
public sealed partial class PopupActionStep : ActionStepBase<PopupActionStep>
{
    /// <summary>
    ///     The gas we're creating
    /// </summary>
    [DataField]
    public LocId Text = "Hello!";
}
