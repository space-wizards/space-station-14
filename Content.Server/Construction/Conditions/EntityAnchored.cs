using System.Threading.Tasks;
using Content.Shared.Construction;
using Content.Shared.Examine;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Construction.Conditions
{
    [UsedImplicitly]
    [DataDefinition]
    public class EntityAnchored : IGraphCondition
    {
        [DataField("anchored")] public bool Anchored { get; private set; } = true;

        public async Task<bool> Condition(IEntity entity)
        {
            if (!entity.TryGetComponent(out IPhysBody? physics)) return false;

            return (physics.BodyType == BodyType.Static && Anchored) || (physics.BodyType != BodyType.Static && !Anchored);
        }

        public bool DoExamine(ExaminedEvent args)
        {
            var entity = args.Examined;

            switch (Anchored)
            {
                case true when !entity.Transform.Anchored:
                    args.PushMarkup("First, anchor it.");
                    return true;
                case false when entity.Transform.Anchored:
                    args.PushMarkup("First, unanchor it.");
                    return true;
            }

            return false;
        }
    }
}
