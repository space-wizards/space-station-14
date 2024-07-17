using Robust.Shared.Map;
using Robust.Shared.Physics.Collision.Shapes;

namespace Content.Server.NPC.Systems;

public sealed class ActuationSystem : EntitySystem
{

}

/*
- type: npcBehavior
  id: Melee
  data:
    entities:
      target: NpcMeleeUtilityQuery
  sequence:
  - !type:NpcMoveTo (this wraps the component)
  - !type:NpcMelee (this wraps the component)

    // Do not access AI knowledge / decisions directly, should be entirely independent.

  Put juking on its own thing.
 */
