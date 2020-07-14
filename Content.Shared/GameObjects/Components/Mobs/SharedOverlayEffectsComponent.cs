using System;
using System.Collections.Generic;
using System.ComponentModel;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timers;
using Robust.Shared.ViewVariables;
using YamlDotNet.RepresentationModel;
using Component = Robust.Shared.GameObjects.Component;

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
    public class OverlayContainer
    {
        [ViewVariables(VVAccess.ReadOnly)]
        public string ID { get; }

        public OverlayContainer([NotNull] string id)
        {
            ID = id;
        }

        public OverlayContainer(OverlayType type) : this(type.ToString())
        {

        }

        public override bool Equals(object obj)
        {
            if (obj is OverlayContainer container)
            {
                return container.ID == ID;
            }

            if (obj is string idString)
            {
                return idString == ID;
            }

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return (ID != null ? ID.GetHashCode() : 0);
        }
    }

    [Serializable, NetSerializable]
    public class OverlayEffectComponentState : ComponentState
    {
        public List<OverlayContainer> Overlays;

        public OverlayEffectComponentState(List<OverlayContainer> overlays) : base(ContentNetIDs.OVERLAYEFFECTS)
        {
            Overlays = overlays;
        }
    }

    [Serializable, NetSerializable]
    public class TimedOverlayContainer : OverlayContainer
    {
        [ViewVariables(VVAccess.ReadOnly)]
        public int Length { get; }

        public TimedOverlayContainer(string id, int length) : base(id)
        {
            Length = length;
        }

        public void StartTimer(Action finished) => Timer.Spawn(Length, finished);
    }

    public enum OverlayType
    {
        GradientCircleMaskOverlay,
        CircleMaskOverlay,
        FlashOverlay
    }
}
