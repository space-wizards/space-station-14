// Interfaces for the SpacemanDMM linter, define'd to nothing when the linter
// is not in use.

// The SPACEMAN_DMM define is set by the linter and other tooling when it runs.
#ifdef SPACEMAN_DMM
	#define RETURN_TYPE(X) set SpacemanDMM_return_type = X
	#define SHOULD_CALL_PARENT(X) set SpacemanDMM_should_call_parent = X
	#define UNLINT(X) SpacemanDMM_unlint(X)
#else
	#define RETURN_TYPE(X)
	#define SHOULD_CALL_PARENT(X)
	#define UNLINT(X) X
#endif

/world/proc/enable_debugger()
    var/dll = world.GetConfig("env", "EXTOOLS_DLL")
    if (dll)
        call(dll, "debug_initialize")()
