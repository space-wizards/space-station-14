using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client.IconSmoothing;

[ImplicitDataDefinitionForInheritors]
public partial interface ISpriteSmoothState
{
    /// <summary>
    /// The Base string that we use to build the desired sprite state.
    /// </summary>
    public string Base { get; protected set;  }

    /// <summary>
    /// List of keys that this sprite state smooths with.
    /// </summary>
    HashSet<string> Mask { get; protected set;  }

    /// <summary>
    /// They key used for getting sprite states from <see cref="SpriteComponent"/>
    /// Should be given a default, and only needs to be edited if multiple states exist.
    /// </summary>
    string LayerKey { get; protected set; }

    /// <summary>
    /// Index override for our icon smooth layers. If null, layers will appear on top.
    /// </summary>
    int? Index { get; protected set;  }

    ProtoId<ShaderPrototype>? Shader { get; protected set;  }

    void InitializeStates(Entity<SpriteComponent> entity, SpriteSystem sprite);

    IEnumerable<(string key, string state)> EnumerateStates(HashSet<string>?[] layers);
}
