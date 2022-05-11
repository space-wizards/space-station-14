using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Maps.NameGenerators;

[ImplicitDataDefinitionForInheritors]
public abstract class StationNameGenerator
{
    public abstract string FormatName(string input);
}
