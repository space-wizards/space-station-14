
/*

Contents:
- Definitions, because the original Ninja code has so much magic.

*/


//ninjacost() specificCheck defines
#define N_STEALTH_CANCEL	1
#define N_SMOKE_BOMB		2
#define N_ADRENALINE		3

//ninjaDrainAct() defines for non numerical returns
//While not strictly needed, it's nicer than them just returning "twat"
//Which was my original intention.

#define INVALID_DRAIN			"INVALID" //This one is if the drain proc needs to cancel, eg missing variables, etc, it's important.

#define DRAIN_RD_HACK_FAILED	"RDHACKFAIL"
#define DRAIN_MOB_SHOCK			"MOBSHOCK"
#define DRAIN_MOB_SHOCK_FAILED	"MOBSHOCKFAIL"
