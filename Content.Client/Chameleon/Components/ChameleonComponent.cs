namespace Content.Client.Chameleon.Components;

[RegisterComponent]
public sealed class ChameleonComponent : Component
{
    /// <summary>
    ///     Whether or not the entity previously had an interaction outline prior to cloaking.
    /// </summary>
    [DataField("hadOutline")]
    public bool HadOutline;

    [ViewVariables]
    public float Speed;
}
