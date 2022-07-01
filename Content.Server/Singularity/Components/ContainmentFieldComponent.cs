using Content.Shared.Singularity.Components;

namespace Content.Server.Singularity.Components;
[RegisterComponent]
[ComponentReference(typeof(SharedContainmentFieldComponent))]
public sealed class ContainmentFieldComponent : SharedContainmentFieldComponent
{
    public ContainmentFieldConnection? Parent; //dont know if needed

    /// <summary>
    /// How far should this field check before giving up?
    /// Maybe best put on the generator? Dunno
    /// </summary>
    public int MaxDistance;

    /// <summary>
    /// Store a direction + field?
    /// Also maybe better put on the generator.
    /// </summary>
    public readonly Dictionary<Direction, ContainmentFieldComponent> Fields = new();

    /// <summary>
    /// The first generator this field is connected to
    /// </summary>
    public ContainmentFieldGeneratorComponent? Gen1;

    /// <summary>
    /// The second field this generator is connected to
    /// </summary>
    public ContainmentFieldGeneratorComponent? Gen2;
}

public sealed class ContainmentFieldConnectEvent : EntityEventArgs
{
    public ContainmentFieldGeneratorComponent Generator1;
    public ContainmentFieldGeneratorComponent Generator2;

    public ContainmentFieldConnectEvent(ContainmentFieldGeneratorComponent generator1, ContainmentFieldGeneratorComponent generator2)
    {
        Generator1 = generator1;
        Generator2 = generator2;
    }
}
