#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.GameObjects.Components.Body.Mechanism;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Maths;

namespace Content.Shared.GameObjects.Components.Body.Part
{
    public abstract class BodyPartComponent : Component, IBodyPart
    {
        public override string Name => "BodyPart";

        public abstract ISharedBodyManager? Body { get; set; }

        public abstract BodyPartType PartType { get; }

        public abstract string PartName { get; }

        public abstract string Plural { get; }

        public abstract int Size { get; }

        public abstract int MaxDurability { get; }

        public abstract int CurrentDurability { get; }

        public abstract IReadOnlyCollection<IMechanism> Mechanisms { get; }

        public abstract string RSIPath { get; }

        public abstract string RSIState { get; }

        public abstract Enum? RSIMap { get; set; }

        public abstract Color? RSIColor { get; set; }

        public abstract bool IsVital { get; }

        public abstract bool SpawnDropped([NotNullWhen(true)] out IEntity? dropped);

        public abstract bool SurgeryCheck(SurgeryType surgery);

        public abstract bool CanAttachPart(IBodyPart part);

        public abstract bool CanInstallMechanism(IMechanism mechanism);

        public abstract bool TryDropMechanism(IEntity dropLocation, IMechanism mechanismTarget);

        public abstract bool DestroyMechanism(IMechanism mechanism);
    }
}
