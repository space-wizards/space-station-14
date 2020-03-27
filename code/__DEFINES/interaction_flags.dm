/// whether can_interact() checks for anchored. only works on movables.
#define INTERACT_ATOM_REQUIRES_ANCHORED 			(1<<0)
/// calls try_interact() on attack_hand() and returns that.
#define INTERACT_ATOM_ATTACK_HAND 					(1<<1)
/// automatically calls and returns ui_interact() on interact().
#define INTERACT_ATOM_UI_INTERACT 					(1<<2)
/// user must be dextrous
#define INTERACT_ATOM_REQUIRES_DEXTERITY 			(1<<3)
/// ignores incapacitated check
#define INTERACT_ATOM_IGNORE_INCAPACITATED		 	(1<<4)
/// incapacitated check ignores restrained
#define INTERACT_ATOM_IGNORE_RESTRAINED 			(1<<5)
/// incapacitated check checks grab
#define INTERACT_ATOM_CHECK_GRAB 					(1<<6)
/// prevents leaving fingerprints automatically on attack_hand
#define INTERACT_ATOM_NO_FINGERPRINT_ATTACK_HAND	(1<<7)
/// adds hiddenprints instead of fingerprints on interact
#define INTERACT_ATOM_NO_FINGERPRINT_INTERACT 		(1<<8)

/// attempt pickup on attack_hand for items
#define INTERACT_ITEM_ATTACK_HAND_PICKUP (1<<0)

/// can_interact() while open
#define INTERACT_MACHINE_OPEN 				(1<<0)
/// can_interact() while offline
#define INTERACT_MACHINE_OFFLINE 			(1<<1)
/// try to interact with wires if open
#define INTERACT_MACHINE_WIRES_IF_OPEN 		(1<<2)
/// let silicons interact
#define INTERACT_MACHINE_ALLOW_SILICON 		(1<<3)
/// let silicons interact while open
#define INTERACT_MACHINE_OPEN_SILICON 		(1<<4)
/// must be silicon to interact
#define INTERACT_MACHINE_REQUIRES_SILICON	(1<<5)
/// MACHINES HAVE THIS BY DEFAULT, SOMEONE SHOULD RUN THROUGH MACHINES AND REMOVE IT FROM THINGS LIKE LIGHT SWITCHES WHEN POSSIBLE!!--------------------------
/// This flag determines if a machine set_machine's the user when the user uses it, making updateUsrDialog make the user re-call interact() on it.
/// THIS FLAG IS ON ALL MACHINES BY DEFAULT, NEEDS TO BE RE-EVALUATED LATER!!
#define INTERACT_MACHINE_SET_MACHINE 		(1<<6)
