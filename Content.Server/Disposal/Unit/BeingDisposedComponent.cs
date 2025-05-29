namespace Content.Server.Disposal.Unit;

/// <summary>
///     A component added to entities that are currently in disposals.
/// </summary>
[RegisterComponent]
public sealed partial class BeingDisposedComponent : Component
{
    [ViewVariables]
    public EntityUid Holder;
}
