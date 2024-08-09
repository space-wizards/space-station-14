using Content.Server.Genetics.Systems;
using Content.Shared.Genetics;
using Robust.Shared.Prototypes;

namespace Content.Server.Genetics.Components;

/// <summary>
/// Gives this entity a genome for traits to be passed on and potentially mutated.
/// Both of those must be handled by other systems, on its own it has no functionality.
/// </summary>
[RegisterComponent, Access(typeof(GenomeSystem))]
public sealed partial class GenomeComponent : Component
{
    /// <summary>
    /// Name of the <see cref="GenomePrototype"/> to create on init.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<GenomePrototype> GenomeId = string.Empty;

    /// <summary>
    /// Genome layout to use for this round and genome type.
    /// Stored in <see cref="GeneticsSystem"/>.
    /// </summary>
    [ViewVariables]
    public GenomeLayout Layout = new();

    /// <summary>
    /// Genome bits themselves.
    /// Data can be retrieved with <c>_genome.GetInt(comp, "name")</c>, etc.
    /// </summary>
    /// <remarks>
    /// Completely zeroed by default, but at the correct length for the layout.
    /// Another system must use <see cref="GenomeSystem"/> to load genes or copy from a parent.
    /// </remarks>
    [DataField, Access(Other = AccessPermissions.ReadWriteExecute)]
    public Genome Genome = new();
}
