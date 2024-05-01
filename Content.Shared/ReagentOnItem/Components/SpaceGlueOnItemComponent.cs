namespace Content.Shared.ReagentOnItem;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class SpaceGlueOnItemComponent : ReagentOnItemComponent
{
    /// <summary>
    ///     The time you should check if the glue component has run out.
    /// </summary>
    [DataField, AutoPausedField]
    public TimeSpan TimeOfNextCheck;

    /// <summary>
    ///     How long the item will be stuck to someones hand per unit of glue.
    /// </summary>
    [DataField]
    public TimeSpan DurationPerUnit = TimeSpan.FromSeconds(6);
}
