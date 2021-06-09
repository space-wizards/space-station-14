using Content.Shared.Body.Part;
using Content.Shared.Damage;

namespace Content.Server.Body
{
    public interface IBodyHealthChangeParams
    {
        BodyPartType Part { get; }
    }

    public class BodyDamageChangeParams : DamageChangeParams, IBodyHealthChangeParams
    {
        public BodyDamageChangeParams(BodyPartType part)
        {
            Part = part;
        }

        public BodyPartType Part { get; }
    }
}
