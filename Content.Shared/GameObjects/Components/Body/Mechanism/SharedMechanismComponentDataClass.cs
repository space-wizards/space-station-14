#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.GameObjects.Components.Body.Behavior;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.GameObjects.Components.Body.Mechanism
{
    public partial class SharedMechanismComponentDataClass : ISerializationHooks
    {
        [DataField("behaviours", serverOnly: true)] private HashSet<IMechanismBehavior> _behaviorTypes = new();

        [DataClassTarget("behaviours")] public Dictionary<Type, IMechanismBehavior> _behaviors = new();

        public void BeforeSerialization()
        {
            _behaviorTypes = _behaviors.Values.ToHashSet();
        }

        public void AfterDeserialization()
        {
            foreach (var behavior in _behaviorTypes)
            {
                var type = behavior.GetType();

                if (!_behaviors.TryAdd(type, behavior))
                {
                    Logger.Warning($"Duplicate behavior in {nameof(SharedMechanismComponent)}: {type}.");
                    continue;
                }

                IoCManager.InjectDependencies(behavior);
            }
        }
    }
}
