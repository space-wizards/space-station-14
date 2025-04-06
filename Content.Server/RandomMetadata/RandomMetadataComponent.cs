using Content.Shared.Dataset;
using Robust.Shared.Prototypes;

﻿namespace Content.Server.RandomMetadata;

/// <summary>
///     Randomizes the description and/or the name for an entity by creating it from list of dataset prototypes or strings.
/// </summary>
[RegisterComponent]
public sealed partial class RandomMetadataComponent : Component
{
    [DataField]
    public List<ProtoId<LocalizedDatasetPrototype>>? DescriptionSegments;

    [DataField]
    public List<ProtoId<LocalizedDatasetPrototype>>? NameSegments;

    [DataField]
    public string NameSeparator = " ";

    [DataField]
    public string DescriptionSeparator = " ";
}
