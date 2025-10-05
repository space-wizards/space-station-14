using Robust.Shared.Prototypes;

namespace Content.Shared.Silicons.Laws.LawFormats.LawFormatCorruptions;

/// <summary>
/// Abstract Data Definition shared by LawFormatCorruptions. For details see its implementations.
/// </summary>
[ImplicitDataDefinitionForInheritors]
public abstract partial class LawFormatCorruption
{
    public abstract ProtoId<LawFormatPrototype>? FormatToApply();
}