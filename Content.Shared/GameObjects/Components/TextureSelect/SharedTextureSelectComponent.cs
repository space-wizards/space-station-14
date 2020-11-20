using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;

namespace Content.Shared.GameObjects.Components.TextureSelect
{
    public class SharedTextureSelectComponent : Component
    {
        public override string Name => "TextureSelect";

        [Serializable, NetSerializable]
        public enum TextureSelectUiKey
        {
            Key,
        }
    }

    [Serializable, NetSerializable]
    public class TextureSelectBoundUserInterfaceState : BoundUserInterfaceState
    {
        public List<string> Textures;
        public TextureSelectBoundUserInterfaceState(List<string> textures)
        {
            Textures = textures;
        }
    }

    [Serializable, NetSerializable]
    public class TextureSelectMessage : BoundUserInterfaceMessage
    {
        public string Texture;
        public TextureSelectMessage(string texture)
        {
            Texture = texture;
        }
    }
}
