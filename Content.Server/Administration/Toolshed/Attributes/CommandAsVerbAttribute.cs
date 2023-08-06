using Content.Shared.Database;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Server.Administration.Toolshed.Attributes;

public interface ICommandAsVerb
{
    public VerbCategory Category { get; }
    public LogImpact Impact { get; }
    public bool Confirmation => false;
    public string? Texture => null;
    public SpriteSpecifier? Sprite => Texture is not null ? new SpriteSpecifier.Texture(new(Texture)) : null;
}
