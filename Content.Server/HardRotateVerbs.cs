using System;
using Content.Shared.GameObjects.Verbs;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Content.Server
{
    // Mapping tools
    // Uncomment if you need them, I guess.

    /*
    [GlobalVerb]
    public sealed class HardRotateCcwVerb : GlobalVerb
    {
        public override bool RequireInteractionRange => false;

        public override void GetData(IEntity user, IEntity target, VerbData data)
        {
            data.Visibility = VerbVisibility.Visible;
            data.Text = "Rotate CCW";
            data.IconTexture = "/Textures/Interface/VerbIcons/rotate_ccw.svg.192dpi.png";
        }

        public override void Activate(IEntity user, IEntity target)
        {
            target.Transform.LocalRotation += Math.PI / 2;
        }
    }

    [GlobalVerb]
    public sealed class HardRotateCwVerb : GlobalVerb
    {
        public override bool RequireInteractionRange => false;

        public override void GetData(IEntity user, IEntity target, VerbData data)
        {
            data.Visibility = VerbVisibility.Visible;
            data.Text = "Rotate CW";
            data.IconTexture = "/Textures/Interface/VerbIcons/rotate_cw.svg.192dpi.png";
        }

        public override void Activate(IEntity user, IEntity target)
        {
            target.Transform.LocalRotation -= Math.PI / 2;
        }
    }*/
}
