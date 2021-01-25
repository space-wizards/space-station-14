using System.Collections.Generic;
using Content.Server.GameObjects.Components.Cargo.RequestSpecifiers;

namespace Content.Server.GameObjects.Components.Cargo
{
    public class CargoRequest
    {
        public readonly CargoRequestPrototype Prototype;

        private readonly Dictionary<RequestSpecifier, int> _progress = new();

        public CargoRequest(CargoRequestPrototype prototype)
        {
            Prototype = prototype;
            foreach (var rs in prototype.RequestSpecifiers)
            {
                _progress[rs] = 0;
            }
        }
    }
}
