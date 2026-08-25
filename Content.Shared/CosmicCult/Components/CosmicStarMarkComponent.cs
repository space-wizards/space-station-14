using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.CosmicCult.Components;

/// <summary>
/// Component for revealing cosmic cultists to the crew. Also contains data to make cultists float.
/// </summary>
[NetworkedComponent, RegisterComponent, AutoGenerateComponentState]
public sealed partial class CosmicStarMarkComponent : Component
{
    [DataField] public SpriteSpecifier Sprite = new SpriteSpecifier.Rsi(new("/Textures/_ST/CosmicCult/Effects/cult-revealed.rsi"), "vfx");

    [DataField, AutoNetworkedField] public float AnimationTime = 2f;

    [DataField, AutoNetworkedField] public Vector2 Offset = new(0, 0.175f);

    public readonly string AnimationKey = "cosmicFloating";
}

[Serializable, NetSerializable]
public enum CosmicRevealedKey
{
    Key
}
