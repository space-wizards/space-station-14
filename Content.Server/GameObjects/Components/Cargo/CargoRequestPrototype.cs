using System.Collections.Generic;
using Content.Server.GameObjects.Components.Cargo.RequestSpecifiers;
using Robust.Shared.Prototypes;
using YamlDotNet.RepresentationModel;

namespace Content.Server.GameObjects.Components.Cargo
{
    [Prototype("request")]
    public class CargoRequestPrototype : IPrototype
    {
        public string Title { get; private set; }
        public string Description { get; private set; }
        private List<RequestSpecifier> _requestSpecifiers;
        public IReadOnlyList<RequestSpecifier> RequestSpecifiers => _requestSpecifiers;
        public void LoadFrom(YamlMappingNode mapping)
        {
            throw new System.NotImplementedException();
        }
    }
}
