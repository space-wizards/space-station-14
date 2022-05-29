using Content.Shared.Roles;

namespace Content.Server.Clothing.Components
{
    [RegisterComponent]
    public sealed class LoadoutComponent : Component
    {
        [DataField("prototype", required: true)]
        public string Prototype = string.Empty;
    }
}
