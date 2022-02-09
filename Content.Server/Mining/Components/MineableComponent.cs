using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;

namespace Content.Server.Mining.Components;

[RegisterComponent]
[Friend(typeof(MineableSystem))]
public class MineableComponent : Component
{
    public float BaseMineTime = 1.0f;
}
