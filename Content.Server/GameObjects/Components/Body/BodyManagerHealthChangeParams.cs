using Content.Shared.GameObjects.Components.Body.Part;
using Content.Shared.GameObjects.Components.Damage;

namespace Content.Server.GameObjects.Components.Body
{
    public interface IBodyManagerHealthChangeParams
    {
        BodyPartType Part { get; }
    }

    public class BodyManagerHealthChangeParams : HealthChangeParams, IBodyManagerHealthChangeParams
    {
        public BodyManagerHealthChangeParams(BodyPartType part)
        {
            Part = part;
        }

        public BodyPartType Part { get; }
    }
}
