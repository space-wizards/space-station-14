namespace Content.Server.Silicons.Laws.LawFormatCorruptions;

/// <summary>
/// Abstract Data Definition shared by LawFormatCorruptions. For details see its implementations.
/// </summary>
[ImplicitDataDefinitionForInheritors]
public abstract partial class LawFormatCorruption
{
    public abstract string? ApplyFormatCorruption(string toFormat);
}