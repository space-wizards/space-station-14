#nullable enable
using System;
using System.Collections.Generic;
using Content.Shared.Body.Components;
using Content.Shared.Damage;
using Content.Shared.DragDrop;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Xenobiology
{
    public abstract class SharedXenoTubeComponent : Component
    {
        public override string Name => "XenoTube";

        [Serializable, NetSerializable]
        public enum XenoTubeStatus
        {
            Occupied,
            Powered
        }
    }
}
