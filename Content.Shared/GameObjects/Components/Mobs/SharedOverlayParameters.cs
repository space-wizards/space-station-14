using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;



namespace Content.Shared.GameObjects.Components.Mobs
{

    [Serializable, NetSerializable]
    public abstract class OverlayParameter
    {
    }

    [Serializable, NetSerializable]
    public class OverlaySpaceOverlayParameter : OverlayParameter
    {
        [ViewVariables(VVAccess.ReadOnly)]
        public OverlaySpace Space { get; set; }

        public OverlaySpaceOverlayParameter(OverlaySpace space)
        {
            Space = space;
        }
    }

    [Serializable, NetSerializable]
    public class PositionOverlayParameter : OverlayParameter
    {
        [ViewVariables(VVAccess.ReadOnly)]
        public Vector2[] Positions { get; set; }

        public PositionOverlayParameter(Vector2 position)
        {
            Positions = new Vector2[] { position };
        }

        public PositionOverlayParameter(Vector2[] positions)
        {
            Positions = positions;
        }
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

        public string[] RSIPaths { get; set; }
        public string[] States { get; set; }

        public TextureOverlayParameter(string rsiPath, string state)
        {
            RSIPaths = new string[] { rsiPath };
            States = new string[] { state };
        }

        public TextureOverlayParameter(string[] rsiPaths, string[] states)
        {
            RSIPaths = rsiPaths;
            States = states;
        }
    }
}
