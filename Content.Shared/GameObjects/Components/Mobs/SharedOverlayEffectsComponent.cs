using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;



namespace Content.Shared.GameObjects.Components.Mobs
{
    /// <summary>
    /// Full screen overlays; Blindness, death, flash, alcohol etc.
    /// </summary>
    public abstract class SharedOverlayEffectsComponent : Component
    {
        public override string Name => "OverlayEffectsUI";

        public sealed override uint? NetID => ContentNetIDs.OVERLAYEFFECTS;
    }

    [Serializable, NetSerializable]
    public class OverlayContainer : IEquatable<string>, IEquatable<OverlayContainer>
    {
        [ViewVariables(VVAccess.ReadOnly)]
        public string ID { get; }

        [ViewVariables(VVAccess.ReadWrite)]
        public List<OverlayParameter> Parameters { get; } = new List<OverlayParameter>();

        public OverlayContainer([NotNull] string id)
        {
            ID = id;
        }

        public OverlayContainer(SharedOverlayID id) : this(id.ToString())
        {
        }

        public OverlayContainer(SharedOverlayID id, params OverlayParameter[] parameters) : this(id)
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

        public bool Equals(string other)
        {
            return ID == other;
        }

        public bool Equals(OverlayContainer other)
        {
            return ID == other?.ID;
        }

        public override int GetHashCode()
        {
            return ID != null ? ID.GetHashCode() : 0;
        }

    }

    [Serializable, NetSerializable]
    public class OverlayEffectComponentMessage : ComponentMessage
    {
        public List<OverlayContainer> Overlays;

        public OverlayEffectComponentMessage(List<OverlayContainer> overlays)
        {
            Directed = true;
            Overlays = overlays;
        }
    }

    [Serializable, NetSerializable]
    public class ResendOverlaysMessage : ComponentMessage
    {
    }

    [Serializable, NetSerializable]
    public abstract class OverlayParameter
    {
    }

    [Serializable, NetSerializable]
    public class TimedOverlayParameter : OverlayParameter
    {
        [ViewVariables(VVAccess.ReadOnly)]
        public int Length { get; set; }

        public double StartedAt { get; private set; }

        public TimedOverlayParameter(int length)
        {
            Length = length;
            StartedAt = IoCManager.Resolve<IGameTiming>().CurTime.TotalMilliseconds;
        }
    }

    [Serializable, NetSerializable]
    public class TextureOverlayParameter : OverlayParameter
    {
        [ViewVariables(VVAccess.ReadOnly)]

        public string[] Names { get; set; }
        public string[] RSIPaths { get; set; }
        public string[] States { get; set; }


        public TextureOverlayParameter(string[] names, string[] rsiPaths, string[] states)
        {
            Names = names;
            RSIPaths = rsiPaths;
            States = states;
        }

        public Dictionary<string, KeyValuePair<string, string>> ToDictionary()
        {
            if (Names.Length != RSIPaths.Length || Names.Length != States.Length) 
                return null;
            Dictionary<string, KeyValuePair<string, string>> toReturn = new Dictionary<string, KeyValuePair<string, string>>();
            for (int i = 0; i < Names.Length; i++)
            {
                toReturn.Add(Names[i], new KeyValuePair<string, string>(RSIPaths[i], States[i]));
            }
            return toReturn;
        }

    }

 

    public enum SharedOverlayID
    {
        GradientCircleMaskOverlay,
        ColoredScreenBorderOverlay,
        CircleMaskOverlay,
        FlashOverlay,
        RadiationPulseOverlay,
        SingularityOverlay
    }
}
