using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Collections;
using Robust.Shared.Prototypes;

namespace Content.Client.IconSmoothing.Smoothings;

/// <summary>
/// This is effectively a 9 slice, but only using 4 instead of 9.
/// Not a 4 slice, that's apparently a name for toasters...
/// </summary>
public sealed partial class CornerSpriteSmoothing : ISpriteSmoothState
{
    public string StateBase { get; set; }

    public HashSet<string> Mask { get; set; }

    public int? Index { get; set; }

    public ProtoId<ShaderPrototype>? Shader { get; set; }
    public void InitializeStates(Entity<SpriteComponent> sprite, SpriteSystem sys)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<(string key, string state)> EnumerateStates<T>(ValueList<HashSet<string>> layers) where T : Enum
    {
        throw new NotImplementedException();
    }

    public (string, string) TryGetState<T>(Direction direction, HashSet<string> layers) where T : Enum
    {
        throw new NotImplementedException();
    }
}
