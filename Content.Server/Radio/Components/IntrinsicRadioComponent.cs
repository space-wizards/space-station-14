namespace Content.Server.Radio.Components;

/// <summary>
/// Denotes that the entity's radio capabilities are intrinsic, not provided by equipment like a headset.
/// Note that this does not allow the entity to radio. For that, use a RadioableComponent as well.
/// </summary>
[RegisterComponent]
public sealed partial class IntrinsicRadioComponent : Component
{
}
