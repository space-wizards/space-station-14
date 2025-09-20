using System.Linq;
using Content.Server.Antag.Components;

namespace Content.Server.Antag;

/// <summary>
/// Spawns an entity when creating an antag for <see cref="AntagSpawnerComponent"/>.
/// </summary>
public sealed class AntagSpawnerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AntagSpawnerComponent, AntagSelectEntityEvent>(OnSelectEntity);
    }

    private void OnSelectEntity(Entity<AntagSpawnerComponent> ent, ref AntagSelectEntityEvent args)
    {
        if (args.AntagRoles != null && ent.Comp.Prototypes != null)
        {
            foreach (var prototypePair in ent.Comp.Prototypes)
            {
                var antagRole = prototypePair.Key;
                var entityProtos = prototypePair.Value;

                if (args.AntagRoles.Contains(antagRole))
                {
                    var rand = new Random();
                    var randomEntity = entityProtos[rand.Next(entityProtos.Count)];

                    args.Entity = Spawn(randomEntity);
                    return;
                }
            }
        }
        else
            args.Entity = Spawn(ent.Comp.Prototype);
    }
}
