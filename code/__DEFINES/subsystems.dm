//! Defines for subsystems and overlays
//!
//! Lots of important stuff in here, make sure you have your brain switched on
//! when editing this file

//! ## DB defines
/**
  * DB major schema version
  *
  * Update this whenever the db schema changes
  *
  * make sure you add an update to the schema_version stable in the db changelog
  */
#define DB_MAJOR_VERSION 5

/**
  * DB minor schema version
  *
  * Update this whenever the db schema changes
  *
  * make sure you add an update to the schema_version stable in the db changelog
  */
#define DB_MINOR_VERSION 7


//! ## Timing subsystem
/**
  * Don't run if there is an identical unique timer active
  *
  * if the arguments to addtimer are the same as an existing timer, it doesn't create a new timer,
  * and returns the id of the existing timer
  */
#define TIMER_UNIQUE			(1<<0)

///For unique timers: Replace the old timer rather then not start this one
#define TIMER_OVERRIDE			(1<<1)

/**
  * Timing should be based on how timing progresses on clients, not the server.
  *
  * Tracking this is more expensive,
  * should only be used in conjuction with things that have to progress client side, such as
  * animate() or sound()
  */
#define TIMER_CLIENT_TIME		(1<<2)

///Timer can be stopped using deltimer()
#define TIMER_STOPPABLE			(1<<3)

///prevents distinguishing identical timers with the wait variable
///
///To be used with TIMER_UNIQUE
#define TIMER_NO_HASH_WAIT		(1<<4)

///Loops the timer repeatedly until qdeleted
///
///In most cases you want a subsystem instead, so don't use this unless you have a good reason
#define TIMER_LOOP				(1<<5)

///Empty ID define
#define TIMER_ID_NULL -1

//! ## Initialization subsystem

///New should not call Initialize
#define INITIALIZATION_INSSATOMS 0
///New should call Initialize(TRUE)
#define INITIALIZATION_INNEW_MAPLOAD 2
///New should call Initialize(FALSE)
#define INITIALIZATION_INNEW_REGULAR 1

//! ### Initialization hints

///Nothing happens
#define INITIALIZE_HINT_NORMAL 0
/**
  * call LateInitialize at the end of all atom Initalization
  *
  * The item will be added to the late_loaders list, this is iterated over after
  * initalization of subsystems is complete and calls LateInitalize on the atom
  * see [this file for the LateIntialize proc](atom.html#proc/LateInitialize)
  */
#define INITIALIZE_HINT_LATELOAD 1

///Call qdel on the atom after intialization
#define INITIALIZE_HINT_QDEL 2

///type and all subtypes should always immediately call Initialize in New()
#define INITIALIZE_IMMEDIATE(X) ##X/New(loc, ...){\
    ..();\
    if(!(flags_1 & INITIALIZED_1)) {\
        args[1] = TRUE;\
        SSatoms.InitAtom(src, args);\
    }\
}

// Subsystem init_order, from highest priority to lowest priority
// Subsystems shutdown in the reverse of the order they initialize in
// The numbers just define the ordering, they are meaningless otherwise.

#define INIT_ORDER_PROFILER			101
#define INIT_ORDER_TITLE			100
#define INIT_ORDER_GARBAGE			99
#define INIT_ORDER_DBCORE			95
#define INIT_ORDER_BLACKBOX			94
#define INIT_ORDER_SERVER_MAINT		93
#define INIT_ORDER_INPUT			85
#define INIT_ORDER_VIS				80
#define INIT_ORDER_ACHIEVEMENTS		77
#define INIT_ORDER_MATERIALS		76
#define INIT_ORDER_RESEARCH			75
#define INIT_ORDER_EVENTS			70
#define INIT_ORDER_JOBS				65
#define INIT_ORDER_QUIRKS			60
#define INIT_ORDER_TICKER			55
#define INIT_ORDER_MAPPING			50
#define INIT_ORDER_NETWORKS			45
#define INIT_ORDER_ECONOMY			40
#define INIT_ORDER_OUTPUTS			35
#define INIT_ORDER_ATOMS			30
#define INIT_ORDER_LANGUAGE			25
#define INIT_ORDER_MACHINES			20
#define INIT_ORDER_SKILLS			15
#define INIT_ORDER_TIMER			1
#define INIT_ORDER_DEFAULT			0
#define INIT_ORDER_AIR				-1
#define INIT_ORDER_ASSETS			-4
#define INIT_ORDER_ICON_SMOOTHING	-5
#define INIT_ORDER_OVERLAY			-6
#define INIT_ORDER_XKEYSCORE		-10
#define INIT_ORDER_STICKY_BAN		-10
#define INIT_ORDER_LIGHTING			-20
#define INIT_ORDER_SHUTTLE			-21
#define INIT_ORDER_MINOR_MAPPING	-40
#define INIT_ORDER_PATH				-50
#define INIT_ORDER_DISCORD			-60
#define INIT_ORDER_PERSISTENCE		-95
#define INIT_ORDER_CHAT				-100 //Should be last to ensure chat remains smooth during init.

// Subsystem fire priority, from lowest to highest priority
// If the subsystem isn't listed here it's either DEFAULT or PROCESS (if it's a processing subsystem child)

#define FIRE_PRIORITY_PING			10
#define FIRE_PRIORITY_IDLE_NPC		10
#define FIRE_PRIORITY_SERVER_MAINT	10
#define FIRE_PRIORITY_RESEARCH		10
#define FIRE_PRIORITY_VIS			10
#define FIRE_PRIORITY_GARBAGE		15
#define FIRE_PRIORITY_WET_FLOORS	20
#define FIRE_PRIORITY_AIR			20
#define FIRE_PRIORITY_NPC			20
#define FIRE_PRIORITY_PROCESS		25
#define FIRE_PRIORITY_THROWING		25
#define FIRE_PRIORITY_SPACEDRIFT	30
#define FIRE_PRIORITY_FIELDS		30
#define FIRE_PRIOTITY_SMOOTHING		35
#define FIRE_PRIORITY_NETWORKS		40
#define FIRE_PRIORITY_OBJ			40
#define FIRE_PRIORITY_ACID			40
#define FIRE_PRIOTITY_BURNING		40
#define FIRE_PRIORITY_DEFAULT		50
#define FIRE_PRIORITY_PARALLAX		65
#define FIRE_PRIORITY_MOBS			100
#define FIRE_PRIORITY_TGUI			110
#define FIRE_PRIORITY_TICKER		200
#define FIRE_PRIORITY_ATMOS_ADJACENCY	300
#define FIRE_PRIORITY_CHAT			400
#define FIRE_PRIORITY_OVERLAYS		500
#define FIRE_PRIORITY_INPUT			1000 // This must always always be the max highest priority. Player input must never be lost.

// SS runlevels

#define RUNLEVEL_INIT 0
#define RUNLEVEL_LOBBY 1
#define RUNLEVEL_SETUP 2
#define RUNLEVEL_GAME 4
#define RUNLEVEL_POSTGAME 8

#define RUNLEVELS_DEFAULT (RUNLEVEL_SETUP | RUNLEVEL_GAME | RUNLEVEL_POSTGAME)



//! ## Overlays subsystem

///Compile all the overlays for an atom from the cache lists
#define COMPILE_OVERLAYS(A)\
	if (TRUE) {\
		var/list/ad = A.add_overlays;\
		var/list/rm = A.remove_overlays;\
		var/list/po = A.priority_overlays;\
		if(LAZYLEN(rm)){\
			A.overlays -= rm;\
			rm.Cut();\
		}\
		if(LAZYLEN(ad)){\
			A.overlays |= ad;\
			ad.Cut();\
		}\
		if(LAZYLEN(po)){\
			A.overlays |= po;\
		}\
		for(var/I in A.alternate_appearances){\
			var/datum/atom_hud/alternate_appearance/AA = A.alternate_appearances[I];\
			if(AA.transfer_overlays){\
				AA.copy_overlays(A, TRUE);\
			}\
		}\
		A.flags_1 &= ~OVERLAY_QUEUED_1;\
	}
