using Content.Server.UserInterface;
using Content.Shared.Crayon;
using Content.Shared.Sound;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Crayon
{
    [RegisterComponent]
    public sealed class CrayonComponent : SharedCrayonComponent, ISerializationHooks
    {
        [DataField("useSound")] public SoundSpecifier? UseSound;

        [ViewVariables]
        public Color Color { get; private set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public int Charges { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("capacity")]
        public int Capacity { get; set; } = 30;

        [ViewVariables] public BoundUserInterface? UserInterface => Owner.GetUIOrNull(CrayonUiKey.Key);

        void ISerializationHooks.AfterDeserialization()
        {
            Color = Color.FromName(_color);
        }
    }
}
