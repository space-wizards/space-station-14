using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.Interfaces;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Players;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Effects
{
   
    /// <summary>
    ///     Gravitational lensing shader application to all people who can see the singularity.
    /// </summary>
    [RegisterComponent]
    public class SingularityShaderAuraComponent : ServerShaderAuraComponent
    {
        public override string Name => "SingularityShaderAura";

        protected override int Radius => 6;
        protected string CurrentActiveTexturePath = "Objects/Fun/toys.rsi";
        protected string CurrentActiveTextureState = "singularitytoy";

        protected OverlayContainer ActiveOverlay = null;

        protected override void OnEnterRange(ServerOverlayEffectsComponent overlayEffects)
        {
            string[] names = new string[] { "singularityTexture" };
            string[] paths = new string[] { CurrentActiveTexturePath };
            string[] states = new string[] { CurrentActiveTextureState };
            OverlayParameter[] parameters = { new TextureOverlayParameter(names, paths, states) };
            ActiveOverlay = new OverlayContainer(SharedOverlayID.SingularityOverlay, parameters);
            overlayEffects.AddOverlay(ActiveOverlay);
        }

        protected override void OnExitRange(ServerOverlayEffectsComponent overlayEffects)
        {
            overlayEffects.RemoveOverlay(ActiveOverlay);
            ActiveOverlay = null;
        }

        public void ChangeActiveSingularityTexture(string newPath)
        {
            CurrentActiveTexturePath = newPath;
        }
    }
}
