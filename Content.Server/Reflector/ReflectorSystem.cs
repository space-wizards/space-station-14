using System;
using Content.Server.Popups;
using Content.Shared.Audio;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Throwing;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;
using System.Collections.Generic;
using Content.Server.Alert;
using Content.Server.Atmos.Components;
using Content.Server.Stunnable;
using Content.Server.Temperature.Systems;
using Content.Shared.ActionBlocker;
using Content.Shared.Alert;
using Content.Shared.Atmos;
using Content.Shared.Damage;
using Content.Shared.Popups;
using Content.Shared.Temperature;
using Content.Shared.Tag;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Content.Shared.Damage.Prototypes;
using System.Linq;
using Robust.Shared.Maths;
using System.Threading;
using Content.Server.Power.EntitySystems;
using Content.Server.Projectiles.Components;
using Content.Server.Singularity.Components;
using Content.Server.Storage.Components;
using Content.Shared.Singularity.Components;
using Robust.Shared.Log;
using Timer = Robust.Shared.Timing.Timer;


namespace Content.Server.Reflector
{

    public class ReflectorSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            //SubscribeLocalEvent<ReflectorComponent, EndCollideEvent>(OnEndCollide);
            SubscribeLocalEvent<ReflectorComponent, InteractHandEvent>(OnInteractHand);
            SubscribeLocalEvent<ReflectorComponent, PreventCollideEvent>(PreventCollision);
        }

        //this needs to be moved to a ui, and changed so that it can update without changing the angle
        private void OnInteractHand(EntityUid uid, ReflectorComponent reflector, InteractHandEvent args)
        {
            reflector.Angle += 45;
            if (EntityManager.TryGetComponent<SpriteComponent>(args.TargetUid, out SpriteComponent? sprite))
            {
                sprite.LayerSetRotation(1, ToRadians(reflector.Angle));
            }

            //so someone cant sit there for an hour clicking it to crash the server
            if (reflector.Angle >= 360)
            {
                reflector.Angle -= 360;
            }
        }

        private void PreventCollision(EntityUid uid, ReflectorComponent reflector, PreventCollideEvent args)
        {
            Angle ReflectorAngle = ToRadians(reflector.Angle);

            Angle ProjectileAngle = args.BodyB.Owner.Transform.LocalRotation.ToWorldVec().ToWorldAngle();

            Angle Incidence = GetAngleOfIncidence(ReflectorAngle, ProjectileAngle);

            IEntity Bolt = args.BodyB.Owner;

            //this is broken, but its suposed to stop bolts from passing through side on
            if (Math.Abs(Incidence) == ToRadians(180) && !(reflector.Type == 3))
            {
                return;
            }

            if(reflector.Whitelist.IsValid(args.BodyB.OwnerUid))
            {
                Angle NewAngle = 0;

                //this could probably be better
                switch(reflector.Type)
                {
                    case 0:
                        if (Math.Abs(Incidence) < ToRadians(180))
                        {
                            args.Cancel();
                            NewAngle = ReflectorAngle * 2;
                            break;
                        }
                        else
                        {
                            return;
                        }
                    case 1:
                        args.Cancel();
                        NewAngle = ReflectorAngle * 2;
                        break;
                    case 2:
                        args.Cancel();
                        NewAngle = ReflectorAngle;
                        break;
                }

                if (Bolt.Transform.LocalRotation != ReflectorAngle + (Math.PI / 2))
                {
                    //just something for debugging
                    reflector.Owner.PopupMessageEveryone($"{ProjectileAngle.Degrees} \n {Incidence.Degrees} \n {NewAngle.Degrees}");

                    //center the bolt, this should be changed to the point of the mirror it would hit as if you dont do this the bolt flies off at weird angles
                    Bolt.Transform.Coordinates = EntityManager.GetEntity(uid).Transform.Coordinates;

                    //rotate the veloctiy vector
                    args.BodyB.LinearVelocity = (NewAngle).RotateVec(args.BodyB.LinearVelocity);

                    //rotate the bolt so it flies straight
                    Bolt.Transform.LocalRotation = ReflectorAngle + (Math.PI / 2);
                }
            }
        }

        private Angle ToRadians(float Angle)
        {
            return Angle * (Math.PI/180);
        }

        /// <summary>
        ///     this is probably all wrong but its been 3 years since i've done angles
        /// <summary>
        private Angle GetAngleOfIncidence(Angle Normal, Angle Input)
        {
            return (Normal - Input) % (Math.PI / 2);
        }
    }
}
