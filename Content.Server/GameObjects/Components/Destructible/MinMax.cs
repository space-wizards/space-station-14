using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Destructible
{
    public struct MinMax
    {
        [ViewVariables]
        public int Min;

        [ViewVariables]
        public int Max;
    }
}
