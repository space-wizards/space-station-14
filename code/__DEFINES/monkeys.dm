//Monkey defines, placed here so they can be read by other things!

//Mode defines
#define MONKEY_IDLE 			0	// idle
#define MONKEY_HUNT 			1	// found target, hunting
#define MONKEY_FLEE 			2	// free from enemies
#define MONKEY_DISPOSE 			3	// dump body in disposals

#define MONKEY_FLEE_HEALTH 					50	// below this health value the monkey starts to flee from enemies
#define MONKEY_ENEMY_VISION 				9	// how close an enemy must be to trigger aggression
#define MONKEY_FLEE_VISION					4	// how close an enemy must be before it triggers flee
#define MONKEY_ITEM_SNATCH_DELAY 			25	// How long does it take the item to be taken from a mobs hand
#define MONKEY_CUFF_RETALIATION_PROB		20  // Probability monkey will aggro when cuffed
#define MONKEY_SYRINGE_RETALIATION_PROB		20  // Probability monkey will aggro when syringed

// Probability per Life tick that the monkey will:
#define MONKEY_RESIST_PROB 					50	// resist out of restraints
												// when the monkey is idle
#define MONKEY_PULL_AGGRO_PROB 				5		// aggro against the mob pulling it
#define MONKEY_SHENANIGAN_PROB 				5		// chance of getting into mischief, i.e. finding/stealing items
												// when the monkey is hunting
#define MONKEY_ATTACK_DISARM_PROB 			50		// disarm an armed attacker
#define MONKEY_WEAPON_PROB 					20		// if not currently getting an item, search for a weapon around it
#define MONKEY_RECRUIT_PROB 				25		// recruit a monkey near it
#define MONKEY_SWITCH_TARGET_PROB 			25		// switch targets if it sees another enemy

#define MONKEY_RETALIATE_HARM_PROB 			95	// probability for the monkey to aggro when attacked with harm intent
#define MONKEY_RETALIATE_DISARM_PROB 		20 	// probability for the monkey to aggro when attacked with disarm intent

#define MONKEY_HATRED_AMOUNT 				4	// amount of aggro to add to an enemy when they attack user
#define MONKEY_HATRED_REDUCTION_PROB 		25	// probability of reducing aggro by one when the monkey attacks

// how many Life ticks the monkey will fail to:
#define MONKEY_HUNT_FRUSTRATION_LIMIT 		8	// Chase after an enemy before giving up
#define MONKEY_DISPOSE_FRUSTRATION_LIMIT 	16 	// Dispose of a body before giving up

#define MONKEY_AGGRESSIVE_MVM_PROB			0	// If you mass edit monkies to be aggressive. there is a small chance of in-fighting
