using Content.Shared.GameObjects.Components.Body.Part;
using Content.Shared.GameObjects.Components.Damage;

namespace Content.Server.GameObjects.Components.Body
{
    public interface IBodyHealthChangeParams
    {
        BodyPartType Part { get; }
    }

    public class BodyHealthChangeParams : HealthChangeParams, IBodyHealthChangeParams
    {
        public BodyHealthChangeParams(BodyPartType part)
        {
            Part = part;
        }

        public BodyPartType Part { get; }
    }
}
