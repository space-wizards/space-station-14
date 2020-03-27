/*ALL DEFINES FOR AIS, CYBORGS, AND SIMPLE ANIMAL BOTS*/

#define DEFAULT_AI_LAWID "default"

//Bot defines, placed here so they can be read by other things!
#define BOT_STEP_DELAY 4 //Delay between movemements
#define BOT_STEP_MAX_RETRIES 5 //Maximum times a bot will retry to step from its position

#define DEFAULT_SCAN_RANGE		7	//default view range for finding targets.

//Mode defines
#define BOT_IDLE 			0	// idle
#define BOT_HUNT 			1	// found target, hunting
#define BOT_PREP_ARREST 	2	// at target, preparing to arrest
#define BOT_ARREST			3	// arresting target
#define BOT_START_PATROL	4	// start patrol
#define BOT_PATROL			5	// patrolling
#define BOT_SUMMON			6	// summoned by PDA
#define BOT_CLEANING 		7	// cleaning (cleanbots)
#define BOT_REPAIRING		8	// repairing hull breaches (floorbots)
#define BOT_MOVING			9	// for clean/floor/med bots, when moving.
#define BOT_HEALING			10	// healing people (medbots)
#define BOT_RESPONDING		11	// responding to a call from the AI
#define BOT_DELIVER			12	// moving to deliver
#define BOT_GO_HOME			13	// returning to home
#define BOT_BLOCKED			14	// blocked
#define BOT_NAV				15	// computing navigation
#define BOT_WAIT_FOR_NAV	16	// waiting for nav computation
#define BOT_NO_ROUTE		17	// no destination beacon found (or no route)

//Bot types
#define SEC_BOT				(1<<0)	// Secutritrons (Beepsky) and ED-209s
#define MULE_BOT			(1<<1)	// MULEbots
#define FLOOR_BOT			(1<<2)	// Floorbots
#define CLEAN_BOT			(1<<3)	// Cleanbots
#define MED_BOT				(1<<4)	// Medibots
#define HONK_BOT			(1<<5)	// Honkbots & ED-Honks
#define FIRE_BOT			(1<<6)  // Firebots

//AI notification defines
#define		NEW_BORG     1
#define		NEW_MODULE   2
#define		RENAME       3
#define		AI_SHELL     4
#define		DISCONNECT   5

//Assembly defines
#define ASSEMBLY_FIRST_STEP 	0
#define ASSEMBLY_SECOND_STEP 	1
#define ASSEMBLY_THIRD_STEP     2
#define ASSEMBLY_FOURTH_STEP    3
#define ASSEMBLY_FIFTH_STEP     4

#define BORG_LAMP_CD_RESET -1 //special value to reset cyborg's lamp_cooldown
