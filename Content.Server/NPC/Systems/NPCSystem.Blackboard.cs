using Content.Server.NPC.Components;

namespace Content.Server.NPC.Systems;

public sealed partial class NPCSystem
{
    public void SetBlackboard(EntityUid uid, string key, object value, NPCComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
        {
            return;
        }

        var blackboard = component.Blackboard;
        blackboard.SetValue(key, value);
    }
}
