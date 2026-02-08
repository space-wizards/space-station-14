namespace Content.Shared.Silicons.Laws.LawFormats;

/// <summary>
/// Abstract Data Definition shared by LawFormats. For details see its implementations.
/// </summary>
[ImplicitDataDefinitionForInheritors]
public abstract partial class LawFormat
{
    public abstract string ApplyFormat(string toFormat);
}
