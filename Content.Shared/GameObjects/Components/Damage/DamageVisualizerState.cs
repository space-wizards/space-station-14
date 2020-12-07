#nullable enable
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components.Damage
{
    public class DamageVisualizerState : IExposeData
    {
        [ViewVariables] public int Damage { get; private set; }

        [ViewVariables] public string? Sprite { get; private set; }

        [ViewVariables] public string? State { get; private set; }

        [ViewVariables] public int Layer { get; private set; }

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.Damage, "damage", 0);
            serializer.DataField(this, x => x.Sprite, "sprite", null);
            serializer.DataField(this, x => x.State, "state", null);
            serializer.DataField(this, x => x.Layer, "layer", 0);
        }
    }
}
