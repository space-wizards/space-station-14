namespace Content.Server.Maps.NameGenerators;

[ImplicitDataDefinitionForInheritors]
public abstract partial class StationNameGenerator
{
    public abstract string FormatName(string input);
}
