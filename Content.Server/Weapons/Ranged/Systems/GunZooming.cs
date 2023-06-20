/*using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

  public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GunZoomingComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<GunZoomingComponent, InteractUsingEvent>(OnInteractUsing);
    }

private void OnUseInHand(EntityUid uid, GunZoomingComponent comp, UseInHandEvent args)
    {
         if (TryComp<SharedEyeComponent>(entity, out var eye))
            {
                eye.Zoom = component.Zoom;
            }


        if (args.Handled)
            return;

        args.Handled = true;
        
        if (comp.Activated)
        {
            public Vector2 Zoom = new(1.25f, 1.25f);
        }
        else
        {
            eye.Zoom = new(1.0f, 1.0f);
        }
    }
*/