#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.GameObjects.Components.Body.Behavior;
using Content.Shared.Interfaces;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Body.Mechanism
{
    public partial class SharedMechanismComponentDataClass
    {
        [DataClassTarget("behaviours")]
        public Dictionary<Type, IMechanismBehavior>? _behaviors;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            var moduleManager = IoCManager.Resolve<IModuleManager>();

            if (moduleManager.IsServerModule)
            {
                _behaviors ??= new();
                serializer.DataReadWriteFunction(
                    "behaviors",
                    null!,
                    behaviors =>
                    {
                        if (behaviors == null)
                        {
                            return;
                        }

                        foreach (var behavior in behaviors)
                        {
                            var type = behavior.GetType();

                            if (!_behaviors.TryAdd(type, behavior))
                            {
                                Logger.Warning($"Duplicate behavior in {nameof(SharedMechanismComponent)}: {type}.");
                                continue;
                            }

                            IoCManager.InjectDependencies(behavior);
                        }
                    },
                    () => _behaviors.Values.ToList());
                if (_behaviors.Count == 0) _behaviors = null;
            }
        }
    }
}
