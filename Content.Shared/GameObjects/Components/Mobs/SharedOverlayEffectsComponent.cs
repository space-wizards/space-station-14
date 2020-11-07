using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;



namespace Content.Shared.GameObjects.Components.Mobs
{
    /// <summary>
    ///     Manages screen overlays: shader instances that create visual effects such as blindness, drunkenness, etc.
    /// </summary>
    public abstract class SharedOverlayEffectsComponent : Component
    {
        public override string Name => "OverlayEffectsUI";

        public sealed override uint? NetID => ContentNetIDs.OVERLAYEFFECTS;
    }




    /// <summary>
    ///     Data class representing a single, unique instance of an overlay and the parameters that are passed into it.
    /// </summary>
    [Serializable, NetSerializable]
    public class OverlayContainer
    {
        [ViewVariables(VVAccess.ReadOnly)]
        public readonly Guid ID;

        [ViewVariables(VVAccess.ReadOnly)]
        public OverlayType OverlayType { get; }

        [ViewVariables(VVAccess.ReadWrite)]
        public List<OverlayParameter> Parameters { get; } = new List<OverlayParameter>();

        public OverlayContainer(Guid id, OverlayType type)
        {
            ID = id;
            OverlayType = type;
        }

        public OverlayContainer(Guid id, OverlayType type, params OverlayParameter[] parameters) : this(id, type)
        {
            Parameters.AddRange(parameters);
        }

        public bool TryGetOverlayParameter<T>(out T parameter) where T : OverlayParameter
        {
            var found = Parameters.FirstOrDefault(p => p is T);
            if (found != null)
            {
                parameter = found as T;
                return true;
            }

            parameter = default;
            return false;
        }
    }





    [Serializable, NetSerializable]
    public class OverlayEffectsSyncMessage : ComponentMessage
    {
        public List<OverlayContainer> Overlays;

        public OverlayEffectsSyncMessage(List<OverlayContainer> overlays)
        {
            Directed = true;
            Overlays = overlays;
        }
    }

    [Serializable, NetSerializable]
    public class OverlayEffectsUpdateMessage : ComponentMessage
    {
        public Guid ID;
        public OverlayParameter[] Parameters;

        public OverlayEffectsUpdateMessage(Guid id, OverlayParameter[] parameters)
        {
            Directed = true;
            ID = id;
            Parameters = parameters;
        }
    }

    [Serializable, NetSerializable]
    public class RequestOverlayEffectsSyncMessage : ComponentMessage
    {
    }

    // This enum is required for the server to be able to tell the client what overlay to draw, since the server doesn't have a reference to the overlay classes.
    // Client takes the NAME of the enum, so OverlayType.GradientCircleMaskOverlay means that it will try to create an instance of the class GradientCircleMaskOverlay.

    public enum OverlayType
    {
        GradientCircleMaskOverlay,
        ColoredScreenBorderOverlay,
        CircleMaskOverlay,
        FlashOverlay,
        RadiationPulseOverlay,
        SingularityOverlay,
        TextureOverlay
    }
}
