using Content.Server.Atmos.Piping.EntitySystems;
using JetBrains.Annotations;

namespace Content.Server.Atmos.Piping.Components
{
    [RegisterComponent]
    public sealed partial class AtmosPipeColorComponent : Component
    {
        [DataField("color")]
        public Color Color { get; set; } = Color.White;

        [ViewVariables(VVAccess.ReadWrite), UsedImplicitly]
        public Color ColorVV
        {
            get => Color;
            set => EntitySystem.Get<AtmosPipeColorSystem>().SetColor(Owner, this, value);
        }
    }
}
