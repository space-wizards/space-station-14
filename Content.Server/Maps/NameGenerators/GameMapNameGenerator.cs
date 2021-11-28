using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Maps.NameGenerators;

[ImplicitDataDefinitionForInheritors]
public abstract class GameMapNameGenerator
{
    public abstract string FormatName(string input);
}
