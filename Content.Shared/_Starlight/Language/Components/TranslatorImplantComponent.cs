using Content.Shared._Starlight.Language.Components.Translators;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared._Starlight.Language.Components;

/// <summary>
///     An implant that allows the implantee to speak and understand other languages.
/// </summary>
[RegisterComponent]
public sealed partial class TranslatorImplantComponent : BaseTranslatorComponent
{
    /// <summary>
    ///     Whether the implantee knows the languages necessary to speak using this implant.
    /// </summary>
    public bool SpokenRequirementSatisfied = false;

    /// <summary>
    ///     Whether the implantee knows the languages necessary to understand translations of this implant.
    /// </summary>
    public bool UnderstoodRequirementSatisfied = false;
}