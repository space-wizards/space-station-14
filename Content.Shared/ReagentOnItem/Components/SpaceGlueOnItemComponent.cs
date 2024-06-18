using Robust.Shared.GameStates;

namespace Content.Shared.ReagentOnItem;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class SpaceGlueOnItemComponent : ReagentOnItemComponent
{
    /// <summary>
    ///     The time you should check if the glue component has run out.
    /// </summary>
    [DataField, AutoPausedField]
    public TimeSpan TimeOfNextCheck;

    /// <summary>
    ///     The minimum time the item will be stuck to someones hand for one unit of glue.
    /// </summary>
    [DataField]
    public TimeSpan MinimumDurationPerUnit = TimeSpan.FromSeconds(5);

    /// <summary>
    ///     The maximum time the item will be stuck to someones hand for one unit of glue.
    /// </summary>
    [DataField]
    public TimeSpan MaximumDurationPerUnit = TimeSpan.FromSeconds(7);
}
