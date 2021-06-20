using Content.Shared.Body.Part;
using Content.Shared.Damage;

namespace Content.Server.Body
{
    // TODO BODY: Remove and pretend it never existed
    public interface IBodyHealthChangeParams
    {
        BodyPartType Part { get; }
    }

    // TODO BODY: Remove and pretend it never existed
    public class BodyDamageChangeParams : DamageChangeParams, IBodyHealthChangeParams
    {
        public BodyDamageChangeParams(BodyPartType part)
        {
            Part = part;
        }

        public BodyPartType Part { get; }
    }
}
