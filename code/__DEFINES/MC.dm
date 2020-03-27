#define MC_TICK_CHECK ( ( TICK_USAGE > Master.current_ticklimit || src.state != SS_RUNNING ) ? pause() : 0 )

#define MC_SPLIT_TICK_INIT(phase_count) var/original_tick_limit = Master.current_ticklimit; var/split_tick_phases = ##phase_count
#define MC_SPLIT_TICK \
    if(split_tick_phases > 1){\
        Master.current_ticklimit = ((original_tick_limit - TICK_USAGE) / split_tick_phases) + TICK_USAGE;\
        --split_tick_phases;\
    } else {\
        Master.current_ticklimit = original_tick_limit;\
    }

// Used to smooth out costs to try and avoid oscillation.
#define MC_AVERAGE_FAST(average, current) (0.7 * (average) + 0.3 * (current))
#define MC_AVERAGE(average, current) (0.8 * (average) + 0.2 * (current))
#define MC_AVERAGE_SLOW(average, current) (0.9 * (average) + 0.1 * (current))

#define MC_AVG_FAST_UP_SLOW_DOWN(average, current) (average > current ? MC_AVERAGE_SLOW(average, current) : MC_AVERAGE_FAST(average, current))
#define MC_AVG_SLOW_UP_FAST_DOWN(average, current) (average < current ? MC_AVERAGE_SLOW(average, current) : MC_AVERAGE_FAST(average, current))

#define NEW_SS_GLOBAL(varname) if(varname != src){if(istype(varname)){Recover();qdel(varname);}varname = src;}

#define START_PROCESSING(Processor, Datum) if (!(Datum.datum_flags & DF_ISPROCESSING)) {Datum.datum_flags |= DF_ISPROCESSING;Processor.processing += Datum}
#define STOP_PROCESSING(Processor, Datum) Datum.datum_flags &= ~DF_ISPROCESSING;Processor.processing -= Datum

//! SubSystem flags (Please design any new flags so that the default is off, to make adding flags to subsystems easier)

/// subsystem does not initialize.
#define SS_NO_INIT 1

/** subsystem does not fire. */
/// (like can_fire = 0, but keeps it from getting added to the processing subsystems list)
/// (Requires a MC restart to change)
#define SS_NO_FIRE 2

/** subsystem only runs on spare cpu (after all non-background subsystems have ran that tick) */
/// SS_BACKGROUND has its own priority bracket
#define SS_BACKGROUND 4

/// subsystem does not tick check, and should not run unless there is enough time (or its running behind (unless background))
#define SS_NO_TICK_CHECK 8

/** Treat wait as a tick count, not DS, run every wait ticks. */
/// (also forces it to run first in the tick, above even SS_NO_TICK_CHECK subsystems)
/// (implies all runlevels because of how it works)
/// (overrides SS_BACKGROUND)
/// This is designed for basically anything that works as a mini-mc (like SStimer)
#define SS_TICKER 16

/** keep the subsystem's timing on point by firing early if it fired late last fire because of lag */
/// ie: if a 20ds subsystem fires say 5 ds late due to lag or what not, its next fire would be in 15ds, not 20ds.
#define SS_KEEP_TIMING 32

/** Calculate its next fire after its fired. */
/// (IE: if a 5ds wait SS takes 2ds to run, its next fire should be 5ds away, not 3ds like it normally would be)
/// This flag overrides SS_KEEP_TIMING
#define SS_POST_FIRE_TIMING 64

//! SUBSYSTEM STATES
#define SS_IDLE 0		/// aint doing shit.
#define SS_QUEUED 1		/// queued to run
#define SS_RUNNING 2	/// actively running
#define SS_PAUSED 3		/// paused by mc_tick_check
#define SS_SLEEPING 4	/// fire() slept.
#define SS_PAUSING 5 	/// in the middle of pausing

#define SUBSYSTEM_DEF(X) GLOBAL_REAL(SS##X, /datum/controller/subsystem/##X);\
/datum/controller/subsystem/##X/New(){\
    NEW_SS_GLOBAL(SS##X);\
    PreInit();\
}\
/datum/controller/subsystem/##X

#define PROCESSING_SUBSYSTEM_DEF(X) GLOBAL_REAL(SS##X, /datum/controller/subsystem/processing/##X);\
/datum/controller/subsystem/processing/##X/New(){\
    NEW_SS_GLOBAL(SS##X);\
    PreInit();\
}\
/datum/controller/subsystem/processing/##X
