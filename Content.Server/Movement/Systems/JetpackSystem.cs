using Content.Server.Movement.Components;
using Robust.Shared.GameObjects;

namespace Content.Server.Movement.Systems;

public sealed class JetpackSystem : EntitySystem
{
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // If we get enough jetpacks just store this, otherwise probably gucci
        foreach (var comp in EntityManager.EntityQuery<JetpackComponent>())
        {
            if (!comp.Active) continue;

            // TODO: Take gas out or something.
        }
    }

    public void EnableJetpack(JetpackComponent component)
    {
        if (component.Active) return;

        component.Active = true;
    }

    public void DisableJetpack(JetpackComponent component)
    {
        if (!component.Active) return;

        component.Active = false;
    }
}
