using Content.Shared.Singularity.Components;

namespace Content.Server.Singularity.Components;
[RegisterComponent]
[ComponentReference(typeof(SharedContainmentFieldComponent))]
public sealed class ContainmentFieldComponent : SharedContainmentFieldComponent
{
    /// <summary>
    /// How far should this field check before giving up?
    /// </summary>
    [ViewVariables]
    [DataField("maxLength")]
    public float MaxLength = 8F;

    /// <summary>
    /// The fields connected to one another
    /// </summary>
    [ViewVariables]
    public List<EntityUid> Fields = new();

    /// <summary>
    /// What fields should this spawn?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("createdField")]
    public string CreatedField = "ContainmentField";

    /// <summary>
    /// The first generator this field is connected to
    /// </summary>
    [ViewVariables]
    public EntityUid? Generator1;

    /// <summary>
    /// The second field this generator is connected to
    /// </summary>
    [ViewVariables]
    public EntityUid? Generator2;
}

public sealed class ContainmentFieldConnectEvent : EntityEventArgs
{
    public EntityUid Generator1;
    public EntityUid Generator2;

    public ContainmentFieldConnectEvent(EntityUid generator1, EntityUid generator2)
    {
        Generator1 = generator1;
        Generator2 = generator2;
    }
}
