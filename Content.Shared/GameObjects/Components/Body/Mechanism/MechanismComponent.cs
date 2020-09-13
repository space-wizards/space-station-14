#nullable enable
using Content.Shared.GameObjects.Components.Body.Part;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Body.Mechanism
{
    public class MechanismComponent : Component, IMechanism
    {
        public override string Name => "Mechanism";

        public string Id { get; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string ExamineMessage { get; set; } = string.Empty;

        public string RSIPath { get; set; } = string.Empty;

        public string RSIState { get; set; } = string.Empty;

        public int MaxDurability { get; set; }

        public int CurrentDurability { get; set; }

        public int DestroyThreshold { get; set; }

        // TODO
        public int Resistance { get; set; }

        // TODO: OnSizeChanged
        public int Size { get; set; }

        public BodyPartCompatibility Compatibility { get; set; }

        public IBody? Body => Part?.Body;

        public IBodyPart? Part { get; set; }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, m => m.Id, "id", string.Empty);

            serializer.DataField(this, m => m.Description, "description", string.Empty);

            serializer.DataField(this, m => m.ExamineMessage, "examineMessage", string.Empty);

            serializer.DataField(this, m => m.RSIPath, "rsiPath", string.Empty);

            serializer.DataField(this, m => m.RSIState, "rsiState", string.Empty);

            serializer.DataField(this, m => m.MaxDurability, "maxDurability", 10);

            serializer.DataField(this, m => m.CurrentDurability, "currentDurability", MaxDurability);

            serializer.DataField(this, m => m.DestroyThreshold, "destroyThreshold", -MaxDurability);

            serializer.DataField(this, m => m.Resistance, "resistance", 0);

            serializer.DataField(this, m => m.Size, "size", 1);

            serializer.DataField(this, m => m.Compatibility, "compatibility", BodyPartCompatibility.Universal);
        }
    }
}
