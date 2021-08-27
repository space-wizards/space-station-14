using System.Collections.Generic;
using Content.Shared.Body.Components;
using Robust.Shared.GameObjects;

namespace Content.Server.Body.EntitySystems
{
    /// <summary>
    ///     Handles everything related to bodies, their parts & their connections.
    /// </summary>
    public class BodySystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
        }

        public IEnumerable<T> GetComponentsOnMechanisms<T>(SharedBodyComponent body)
            where T : Component
        {
            foreach (var part in body.Parts)
            {
                foreach (var mech in part.Key.Mechanisms)
                {
                    if (mech.Owner.TryGetComponent<T>(out var comp))
                    {
                        yield return comp;
                    }
                }
            }
        }
    }
}
