using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
///     Objective that makes you intercept a document from another trator. Will only be assigned if there is someone with a
///     HoldDocumentObjective.
/// </summary>
[RegisterComponent, Access(typeof(InterceptDocumentConditionSystem))]
public sealed partial class InterceptDocumentObjectiveComponent : Component
{
    [DataField(required: true)]
    public string Title;
    [DataField(required: true)]
    public string Description;
}
