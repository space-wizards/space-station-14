using System.Collections.Generic;
using Robust.Shared.Serialization;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects.Components.Mobs.Body
{
    public enum HumanBodyMapDef
    {
        Head,
        Eyes,
        Mouth,
        Chest,
        LeftHand,
        RightHand,
        LeftArm,
        RightArm,
        Groin,
        LeftLeg,
        RightLeg,
        LeftFeet,
        RightFeet
    }
    public class Human : BodyTemplate
    {

        public override void Initialize(IEntity owner)
        {
            base.Initialize(owner);
            bodyMap = mockBodyFactory();
           
        }

        //TODO:- Hack for testing, refactor into YAML ASAP
        private List<Limb> mockBodyFactory()
        {
            var limbs = new List<Limb>();
            int standard_health = 40;
            //Limb tree
            var chest_children = new List<Limb>();
            var groin_children = new List<Limb>();
            var head_children = new List<Limb>();
            var hand_l_child = new List<Limb>();
            var hand_r_child = new List<Limb>();
            var leg_l_child = new List<Limb>();
            var leg_r_child = new List<Limb>();

            //Organs that exist in several limbs (HACKS)
            var snowflake_mouth = new Tongue();
            snowflake_mouth.mockInit("Human Tongue", standard_health, OrganState.Healthy, Owner, this, "");
            var snowflake_eyes = new Eyes();
            snowflake_eyes.mockInit("Human Eyes", standard_health, OrganState.Healthy, Owner, this, "HumanEyes");
            var snowflake_l_hand = new Hands();
            snowflake_l_hand.mockInit("Human Left Hand", standard_health, OrganState.Healthy, Owner, this, "");
            var snowflake_r_hand = new Hands();
            snowflake_r_hand.mockInit("Human Right Hand", standard_health, OrganState.Healthy, Owner, this, "");
            var snowflake_l_leg = new Legs();
            snowflake_l_leg.mockInit("Human Left Leg", standard_health, OrganState.Healthy, Owner, this, "");
            var snowflake_r_leg = new Legs();
            snowflake_r_leg.mockInit("Human Right Leg", standard_health, OrganState.Healthy, Owner, this, "");

            var brain = new Brain();
            brain.mockInit("Human Brain", standard_health, OrganState.Healthy, Owner, this, "HumanBrain");
            var kidneys = new Kidneys();
            kidneys.mockInit("Human Kidneys", standard_health, OrganState.Healthy, Owner, this, "HumanKidneys");
            var heart = new Heart();
            heart.mockInit("Human Heart", standard_health, OrganState.Healthy, Owner, this, "HumanHeart");
            var lungs = new Lungs();
            lungs.mockInit("Human Lungs", standard_health, OrganState.Healthy, Owner, this, "HumanLungs");
            var liver = new Liver();
            liver.mockInit("Human Liver", standard_health, OrganState.Healthy, Owner, this, "HumanLiver");

            allOrgans = new List<Organ>();
            //it's crucial to have same instance of Organ loaded into these two lists
            allOrgans.Add(snowflake_mouth);
            allOrgans.Add(snowflake_eyes);
            allOrgans.Add(snowflake_l_hand);
            allOrgans.Add(snowflake_r_hand);
            allOrgans.Add(snowflake_l_leg);
            allOrgans.Add(snowflake_r_leg);
            allOrgans.Add(brain);
            allOrgans.Add(kidneys);
            allOrgans.Add(heart);
            allOrgans.Add(lungs);

            //Head
            var head_organs = new List<Organ>();
            head_organs.Add(brain);
            head_organs.Add(snowflake_mouth);
            head_organs.Add(snowflake_eyes);
            var head = new Limb("Head", HumanBodyMapDef.Head, head_organs, head_children, standard_health, "HumanHead", "Mob/UI/Human/head.png", Owner);
            chest_children.Add(head);
            limbs.Add(head);

            //Hands
            var arm_l_organs = new List<Organ>();
            arm_l_organs.Add(snowflake_l_hand);
            var arm_l = new Limb("Left Arm", HumanBodyMapDef.LeftHand, arm_l_organs, new List<Limb>(), standard_health, "HumanLArm", "Mob/UI/Human/l_arm.png", Owner, false, true);
            hand_l_child.Add(arm_l);
            limbs.Add(arm_l);

            var hand_l_organs = new List<Organ>();
            hand_l_organs.Add(snowflake_l_hand);
            var hand_l = new Limb("Left Hand", HumanBodyMapDef.LeftHand, hand_l_organs, hand_l_child, standard_health, "HumanLHand", "Mob/UI/Human/l_hand.png", Owner);
            chest_children.Add(hand_l);
            limbs.Add(hand_l);

            var arm_r_organs = new List<Organ>();
            arm_r_organs.Add(snowflake_r_hand);
            var arm_r = new Limb("Right Arm", HumanBodyMapDef.RightArm, arm_r_organs, new List<Limb>(), standard_health, "HumanRArm", "Mob/UI/Human/r_arm.png", Owner, false, true);
            hand_r_child.Add(arm_r);
            limbs.Add(arm_r);

            var hand_r_organs = new List<Organ>();
            hand_r_organs.Add(snowflake_r_hand);
            var hand_r = new Limb("Right Hand", HumanBodyMapDef.RightHand, hand_r_organs, hand_r_child, standard_health, "HumanRHand", "Mob/UI/Human/r_hand.png", Owner);
            chest_children.Add(hand_r);
            limbs.Add(hand_r);

            //Legs
            var foot_l_organs = new List<Organ>();
            foot_l_organs.Add(snowflake_l_leg);
            var foot_l = new Limb("Left Foot", HumanBodyMapDef.LeftFeet, foot_l_organs, new List<Limb>(), standard_health, "HumanLFoot", "Mob/UI/Human/l_foot.png", Owner);
            leg_l_child.Add(foot_l);
            limbs.Add(foot_l);

            var leg_l_organs = new List<Organ>();
            leg_l_organs.Add(snowflake_l_leg);
            var leg_l = new Limb("Left Leg", HumanBodyMapDef.LeftLeg, leg_l_organs, leg_l_child, standard_health, "HumanLLeg", "Mob/UI/Human/l_leg.png", Owner, false, true);
            groin_children.Add(leg_l);
            limbs.Add(leg_l);

            var foot_r_organs = new List<Organ>();
            foot_r_organs.Add(snowflake_r_leg);
            var foot_r = new Limb("Right Foot", HumanBodyMapDef.RightFeet, foot_r_organs, new List<Limb>(), standard_health, "HumanRFoot", "Mob/UI/Human/r_foot.png", Owner);
            leg_r_child.Add(foot_r);
            limbs.Add(foot_r);

            var leg_r_organs = new List<Organ>();
            leg_r_organs.Add(snowflake_r_leg);
            var leg_r = new Limb("Right Leg", HumanBodyMapDef.RightLeg, leg_r_organs, leg_r_child, standard_health, "HumanRLeg", "Mob/UI/Human/r_leg.png", Owner, false, true);
            groin_children.Add(leg_r);
            limbs.Add(leg_r);

            //Groin
            var groin_organs = new List<Organ>();
            groin_organs.Add(kidneys);
            var groin = new Limb("Groin", HumanBodyMapDef.Groin, head_organs, groin_children, standard_health, "HumanGroin", "Mob/UI/Human/groin.png", Owner);
            chest_children.Add(groin);
            limbs.Add(groin);

            //Chest, the root of all limbs
            var chest_organs = new List<Organ>();
            chest_organs.Add(heart);
            chest_organs.Add(lungs);
            chest_organs.Add(liver);
            var chest = new Limb("Chest", HumanBodyMapDef.Chest, chest_organs, chest_children, standard_health, "HumanChest", "Mob/UI/Human/chest.png", Owner);
            limbs.Add(chest);

            return limbs;
        }
    }
}
