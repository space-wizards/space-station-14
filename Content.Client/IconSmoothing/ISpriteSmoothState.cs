using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Collections;
using Robust.Shared.Prototypes;

namespace Content.Client.IconSmoothing;

/// <inheritdoc cref="ISpriteSmoothState{T}"/>
[ImplicitDataDefinitionForInheritors]
public partial interface ISpriteSmoothState
{
    /// <summary>
    /// The Base IconState that we use to build the desired icon-state.
    /// </summary>
    [DataField(required:true)]
    public string StateBase { get; protected set;  }

    /// <summary>
    /// List of keys that this sprite state smooths with.
    /// </summary>
    [DataField(required:true)]
    HashSet<string> Mask { get; protected set;  }

    /// <summary>
    /// Index override for our icon smooth layers. If null, layers will appear on top.
    /// </summary>
    [DataField]
    int? Index { get; protected set;  }

    [DataField]
    ProtoId<ShaderPrototype>? Shader { get; protected set;  }

    void InitializeStates(Entity<SpriteComponent> sprite, SpriteSystem sys);

    IEnumerable<(string key, string state)> EnumerateStates<T>(ValueList<HashSet<string>> layers) where T : Enum;

    (string, string) TryGetState<T>(Direction direction, HashSet<string> layers) where T : Enum;
}
