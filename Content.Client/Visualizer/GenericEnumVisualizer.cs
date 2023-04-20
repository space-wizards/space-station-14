using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.Visualizer
{
    [UsedImplicitly]
    public sealed class GenericEnumVisualizer : AppearanceVisualizer, ISerializationHooks
    {
        public Enum Key { get; set; } = default!;

        public Dictionary<object, string> States { get; set; } = default!;

        [DataField("layer")]
        public int Layer { get; set; } = 0;

        [DataField("key", readOnly: true, required: true)]
        private string _keyRaw = default!;

        [DataField("states", readOnly: true, required: true)]
        private Dictionary<string, string> _statesRaw { get; set; } = default!;

        void ISerializationHooks.AfterDeserialization()
        {
            var reflectionManager = IoCManager.Resolve<IReflectionManager>();

            object ResolveRef(string raw)
            {
                if (reflectionManager.TryParseEnumReference(raw, out var @enum))
                {
                    return @enum;
                }
                else
                {
                    Logger.WarningS("c.c.v.genum", $"Unable to convert enum reference: {raw}");
                }

                return raw;
            }

            // It's important that this conversion be done here so that it may "fail-fast".
            Key = (Enum) ResolveRef(_keyRaw);
            States = _statesRaw.ToDictionary(kvp => ResolveRef(kvp.Key), kvp => kvp.Value);
        }

        [Obsolete("Subscribe to AppearanceChangeEvent instead.")]
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var entities = IoCManager.Resolve<IEntityManager>();
            if (!entities.TryGetComponent(component.Owner, out SpriteComponent? sprite)) return;
            if (!component.TryGetData(Key, out object status)) return;
            if (!States.TryGetValue(status, out var val)) return;
            sprite.LayerSetState(Layer, val);
        }
    }
}
