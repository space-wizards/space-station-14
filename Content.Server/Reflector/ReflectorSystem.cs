using System;
using Robust.Shared.Maths;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Dynamics;
using Content.Server.Popups;
using Content.Shared.Interaction;


namespace Content.Server.Reflector
{

    public class ReflectorSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

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

            // this is actually the normal of the reflector, so a single sided reflector pointing like --^-- is 90 degrees, this is helpfull for boxes
            Angle ReflectorAngle = ToRadians(reflector.Angle);

            // using args.BodyB.Owner.Transform.LocalRotation.ToWorldVec().ToWorldAngle() gets bolts going south saying they're pointing 0 + some tiny fraction
            // south is 270 and this seems to get rid of the float error
            // i also cant seem to use the velocity angle because every time i restart the server it spits out a new angle for a bolt going south
            Angle ProjectileAngle = (args.BodyB.Owner.Transform.LocalRotation + ToRadians(270)) % Math.Tau;

            Angle Incidence = GetAngleOfIncidence(ReflectorAngle, ProjectileAngle);

            // a special tool that will help us later
            IEntity Bolt = args.BodyB.Owner;

            //stops bolts from passing when hitting the side of the mirror
            if (Math.Abs(Incidence) == ToRadians(90) && !(reflector.Type == 3))
            {
                return;
            }

            // we only care if its an emitter bolt, or whatever else the admeme cares to add to the whitelist
            if(reflector.Whitelist.IsValid(args.BodyB.OwnerUid))
            {
                // the bolt gets rotated by this counterclockwise
                // so 200 rotated by 50 gets you 150
                Angle NewAngle = 0;

                // this could probably be better
                // using Incidence or Incidence * 2 for newangle gets the bolt stuck in the reflector
                // if newangle is set to just ReflectorAngle its too shallow because we're not setting the vector to NewAngle, we're rotating it by NewAngle
                switch(reflector.Type)
                {
                    case 0:
                        if (Math.Abs(Incidence) < ToRadians(90))
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
                        //i honestly have no idea why i have to add 90 degrees
                        NewAngle = ReflectorAngle + (Math.PI / 4);
                        break;
                }

                // checks to see if we already rotated the bolt, it works when Incidence == 0 because it'll exit 180 degrees of where it entered
                if (Bolt.Transform.WorldRotation != ReflectorAngle * 2)
                {
                    // just something for debugging
                    reflector.Owner.PopupMessageEveryone($"{ProjectileAngle.Degrees} \n {Incidence.Degrees}");

                    // center the bolt around the mirror, this should be changed to the point of the mirror it would hit because if you just use the bolt's position the bolt flies off at weird angles
                    Bolt.Transform.Coordinates = EntityManager.GetEntity(uid).Transform.Coordinates;

                    // rotate the veloctiy vector
                    //this works when the bolt comes in from the north but breaks when it comes in from the west
                    // im guessing i should do something with the sign but the case of (Math.Sign(Incidence = 0) * NewAngle) makes head hurt
                    args.BodyB.LinearVelocity = NewAngle.RotateVec(args.BodyB.LinearVelocity);

                    // rotate the bolt so it flies straight //broke
                    Bolt.Transform.WorldRotation = NewAngle;
                }
            }
        }

        // im gonna replace everything with pi and tau
        private Angle ToRadians(float Angle)
        {
            return Angle * (Math.PI/180);
        }

        /// <summary>
        ///     get the difference between the bolt angle and the reflector normal
        /// <summary>
        private Angle GetAngleOfIncidence(Angle Normal, Angle Input)
        {
            return ((Input + Math.PI) % Math.Tau) - Normal;
        }
    }
}
