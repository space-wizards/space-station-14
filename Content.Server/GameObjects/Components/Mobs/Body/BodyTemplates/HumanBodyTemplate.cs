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
            int standard_health = 500;
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
            snowflake_mouth.mockInit("Human Mouth", standard_health, OrganState.Healthy, Owner);
            var snowflake_eyes = new Eyes();
            snowflake_eyes.mockInit("Human Tongue", standard_health, OrganState.Healthy, Owner);
            var snowflake_l_hand = new Hands();
            snowflake_l_hand.mockInit("Human Left Hand", standard_health, OrganState.Healthy, Owner);
            var snowflake_r_hand = new Hands();
            snowflake_r_hand.mockInit("Human Right Hand", standard_health, OrganState.Healthy, Owner);
            var snowflake_l_leg = new Legs();
            snowflake_l_leg.mockInit("Human Left Leg", standard_health, OrganState.Healthy, Owner);
            var snowflake_r_leg = new Legs();
            snowflake_r_leg.mockInit("Human Right Leg", standard_health, OrganState.Healthy, Owner);

            var brain = new Brain();
            brain.mockInit("Human Brain", standard_health, OrganState.Healthy, Owner);
            var kidneys = new Kidneys();
            kidneys.mockInit("Human Kidneys", standard_health, OrganState.Healthy, Owner);
            var heart = new Heart();
            heart.mockInit("Human Heart", standard_health, OrganState.Healthy, Owner);
            var lungs = new Lungs();
            lungs.mockInit("Human Lungs", standard_health, OrganState.Healthy, Owner);
            var liver = new Liver();
            liver.mockInit("Human Liver", standard_health, OrganState.Healthy, Owner);

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

            //Eyesocket
            var eyesocket_organs = new List<Organ>();
            eyesocket_organs.Add(snowflake_eyes);
            var eyesocket = new Limb("Eyes", HumanBodyMapDef.Eyes, eyesocket_organs, new List<Limb>(), standard_health);
            head_children.Add(eyesocket);
            limbs.Add(eyesocket);

            //Smash mouth
            var mouth_organs = new List<Organ>();
            mouth_organs.Add(snowflake_mouth);
            var mouth = new Limb("Mouth", HumanBodyMapDef.Mouth, mouth_organs, new List<Limb>(), standard_health);
            head_children.Add(mouth);
            limbs.Add(mouth);

            //Head
            var head_organs = new List<Organ>();
            head_organs.Add(brain);
            head_organs.Add(snowflake_mouth);
            head_organs.Add(snowflake_eyes);
            var head = new Limb("Head", HumanBodyMapDef.Head, head_organs, head_children, standard_health);
            chest_children.Add(head);
            limbs.Add(head);

            //Hands
            var arm_l_organs = new List<Organ>();
            arm_l_organs.Add(snowflake_l_hand);
            var arm_l = new Limb("Left Arm", HumanBodyMapDef.LeftHand, arm_l_organs, new List<Limb>(), standard_health);
            hand_l_child.Add(arm_l);
            limbs.Add(arm_l);

            var hand_l_organs = new List<Organ>();
            hand_l_organs.Add(snowflake_l_hand);
            var hand_l = new Limb("Left Hand", HumanBodyMapDef.LeftHand, hand_l_organs, hand_l_child, standard_health);
            chest_children.Add(hand_l);
            limbs.Add(hand_l);

            var arm_r_organs = new List<Organ>();
            arm_r_organs.Add(snowflake_r_hand);
            var arm_r = new Limb("Right Arm", HumanBodyMapDef.RightArm, arm_r_organs, new List<Limb>(), standard_health);
            hand_r_child.Add(arm_r);
            limbs.Add(arm_r);

            var hand_r_organs = new List<Organ>();
            hand_r_organs.Add(snowflake_r_hand);
            var hand_r = new Limb("Right Hand", HumanBodyMapDef.RightHand, hand_r_organs, hand_r_child, standard_health);
            chest_children.Add(hand_r);
            limbs.Add(hand_r);

            //Legs
            var foot_l_organs = new List<Organ>();
            foot_l_organs.Add(snowflake_l_leg);
            var foot_l = new Limb("Left Foot", HumanBodyMapDef.LeftFeet, foot_l_organs, new List<Limb>(), standard_health);
            leg_l_child.Add(foot_l);
            limbs.Add(foot_l);

            var leg_l_organs = new List<Organ>();
            leg_l_organs.Add(snowflake_l_leg);
            var leg_l = new Limb("Left Leg", HumanBodyMapDef.LeftLeg, leg_l_organs, leg_l_child, standard_health);
            groin_children.Add(leg_l);
            limbs.Add(leg_l);

            var foot_r_organs = new List<Organ>();
            foot_r_organs.Add(snowflake_r_leg);
            var foot_r = new Limb("Right Foot", HumanBodyMapDef.RightFeet, foot_r_organs, new List<Limb>(), standard_health);
            leg_r_child.Add(foot_r);
            limbs.Add(foot_r);

            var leg_r_organs = new List<Organ>();
            leg_r_organs.Add(snowflake_r_leg);
            var leg_r = new Limb("Right Leg", HumanBodyMapDef.RightLeg, leg_r_organs, leg_r_child, standard_health);
            groin_children.Add(leg_r);
            limbs.Add(leg_r);

            //Groin
            var groin_organs = new List<Organ>();
            groin_organs.Add(kidneys);
            var groin = new Limb("Groin", HumanBodyMapDef.Groin, head_organs, groin_children, standard_health);
            chest_children.Add(groin);
            limbs.Add(groin);

            //Chest, the root of all limbs
            var chest_organs = new List<Organ>();
            chest_organs.Add(heart);
            chest_organs.Add(lungs);
            chest_organs.Add(liver);
            var chest = new Limb("Chest", HumanBodyMapDef.Chest, chest_organs, chest_children, standard_health);
            limbs.Add(chest);

            return limbs;
        }
    }
}
