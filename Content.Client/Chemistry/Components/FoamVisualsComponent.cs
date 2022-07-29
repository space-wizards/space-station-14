using Robust.Client.Animations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Client.Chemistry.Components;

[RegisterComponent]
public sealed class FoamVisualsComponent : Component
{
    public string AnimationKey = "foamdissolve_animation";

    [DataField("animationTime")]
    public float Delay = 0.6f;

    [DataField("animationState")]
    public string State = "foam-dissolve";

    public Animation FoamDissolve = new();
}
