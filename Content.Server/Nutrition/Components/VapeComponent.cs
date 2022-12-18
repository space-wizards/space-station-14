using Content.Server.Nutrition.EntitySystems;
using System.Threading;

namespace Content.Server.Nutrition.Components
{
    [RegisterComponent, Access(typeof(VapeSystem))] 
    public sealed class VapeComponent : Component
    {
        [DataField("delay")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float Delay { get; set; } = 5;

        public CancellationTokenSource? CancelToken;
    }
}