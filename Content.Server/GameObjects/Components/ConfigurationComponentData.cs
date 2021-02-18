#nullable enable
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.GameObjects.Components
{
    public partial class ConfigurationComponentData : ISerializationHooks
    {
        [DataField("keys")] private List<string> _keys = new();

        [DataClassTarget("config")] public Dictionary<string, string> Config = new();

        public void BeforeSerialization()
        {
            _keys = Config.Keys.ToList();
        }

        public void AfterDeserialization()
        {
            foreach (var key in _keys)
            {
                Config.Add(key, "");
            }
        }
    }
}
