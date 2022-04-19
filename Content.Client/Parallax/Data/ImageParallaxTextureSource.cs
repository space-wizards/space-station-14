using System.Threading.Tasks;
﻿using System.Collections.Generic;
using Content.Client.Resources;
using Content.Client.IoC;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Content.Client.Parallax.Data
{
    [ImplicitDataDefinitionForInheritors]
    public class ImageParallaxTextureSource : IParallaxTextureSource
    {
        /// <summary>
        /// Texture path.
        /// </summary>
        [DataField("path", required: true)]
        public ResourcePath Path { get; } = default!;

        Task<Texture> IParallaxTextureSource.GenerateTexture()
        {
            return new Task<Texture>(() => StaticIoC.ResC.GetTexture(Path));
        }
    }
}

