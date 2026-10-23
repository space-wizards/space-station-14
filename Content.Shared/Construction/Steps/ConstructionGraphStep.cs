using Content.Shared.EntityEffects;
using Content.Shared.Examine;

namespace Content.Shared.Construction.Steps
{
    [Serializable]
    [ImplicitDataDefinitionForInheritors]
    public abstract partial class ConstructionGraphStep
    {
        [DataField("doAfter")] public float DoAfter { get; private set; }

        [DataField]
        public EntityEffect[] Completed = Array.Empty<EntityEffect>();

        public abstract void DoExamine(ExaminedEvent examinedEvent);
        public abstract ConstructionGuideEntry GenerateGuideEntry();
    }
}
