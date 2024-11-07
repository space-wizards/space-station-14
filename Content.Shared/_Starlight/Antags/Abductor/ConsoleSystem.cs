using System.Linq;
using Content.Shared.Bed.Sleep;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Events;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Shared.Starlight.Antags.Abductor;

public abstract class SharedAbductorSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
    }

    public override void Update(float frameTime)
    {
    }
}

