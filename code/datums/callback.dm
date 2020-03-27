/**
  *# Callback Datums
  *A datum that holds a proc to be called on another object, used to track proccalls to other objects
  *
  * ## USAGE
  *
  * ```
  * var/datum/callback/C = new(object|null, /proc/type/path|"procstring", arg1, arg2, ... argn)
  * var/timerid = addtimer(C, time, timertype)
  * you can also use the compiler define shorthand
  * var/timerid = addtimer(CALLBACK(object|null, /proc/type/path|procstring, arg1, arg2, ... argn), time, timertype)
  * ```
  * 
  * Note: proc strings can only be given for datum proc calls, global procs must be proc paths
  *
  * Also proc strings are strongly advised against because they don't compile error if the proc stops existing
  *
  * In some cases you can provide a shortform of the procname, see the proc typepath shortcuts documentation below
  *
  * ## INVOKING THE CALLBACK
  *`var/result = C.Invoke(args, to, add)` additional args are added after the ones given when the callback was created
  *
  * `var/result = C.InvokeAsync(args, to, add)` Asyncronous - returns . on the first sleep then continues on in the background
  * after the sleep/block ends, otherwise operates normally.
  *
  * ## PROC TYPEPATH SHORTCUTS
  * (these operate on paths, not types, so to these shortcuts, datum is NOT a parent of atom, etc...)
  *
  * ### global proc while in another global proc:
  * .procname
  *
  * `CALLBACK(GLOBAL_PROC, .some_proc_here)`
  * 
  * ### proc defined on current(src) object (when in a /proc/ and not an override) OR overridden at src or any of it's parents:
  * .procname
  *
  * `CALLBACK(src, .some_proc_here)`
  *
  * ### when the above doesn't apply:
  *.proc/procname
  * 
  * `CALLBACK(src, .proc/some_proc_here)`
  * 
  *
  * proc defined on a parent of a some type
  *
  * `/some/type/.proc/some_proc_here`
  *
  * Otherwise you must always provide the full typepath of the proc (/type/of/thing/proc/procname)
  */
/datum/callback

	///The object we will be calling the proc on 
	var/datum/object = GLOBAL_PROC
	///The proc we will be calling on the object
	var/delegate
	///A list of arguments to pass into the proc
	var/list/arguments
	///A weak reference to the user who triggered this callback
	var/datum/weakref/user

/**
  * Create a new callback datum
  *
  * Arguments
  * * thingtocall the object to call the proc on
  * * proctocall the proc to call on the target object
  * * ... an optional list of extra arguments to pass to the proc
  */
/datum/callback/New(thingtocall, proctocall, ...)
	if (thingtocall)
		object = thingtocall
	delegate = proctocall
	if (length(args) > 2)
		arguments = args.Copy(3)
	if(usr)
		user = WEAKREF(usr)
/**
  * Immediately Invoke proctocall on thingtocall, with waitfor set to false
  *
  * Arguments:
  * * thingtocall Object to call on
  * * proctocall Proc to call on that object
  * * ... optional list of arguments to pass as arguments to the proc being called
  */
/world/proc/ImmediateInvokeAsync(thingtocall, proctocall, ...)
	set waitfor = FALSE

	if (!thingtocall)
		return

	var/list/calling_arguments = length(args) > 2 ? args.Copy(3) : null

	if (thingtocall == GLOBAL_PROC)
		call(proctocall)(arglist(calling_arguments))
	else
		call(thingtocall, proctocall)(arglist(calling_arguments))

/**
  * Invoke this callback
  * 
  * Calls the registered proc on the registered object, if the user ref
  * can be resolved it also inclues that as an arg
  *
  * If the datum being called on is varedited, the call is wrapped via WrapAdminProcCall
  */
/datum/callback/proc/Invoke(...)
	if(!usr)
		var/datum/weakref/W = user
		if(W)
			var/mob/M = W.resolve()
			if(M)
				if (length(args))
					return world.PushUsr(arglist(list(M, src) + args))
				return world.PushUsr(M, src)

	if (!object)
		return

	var/list/calling_arguments = arguments
	if (length(args))
		if (length(arguments))
			calling_arguments = calling_arguments + args //not += so that it creates a new list so the arguments list stays clean
		else
			calling_arguments = args
	if(datum_flags & DF_VAR_EDITED)
		return WrapAdminProcCall(object, delegate, calling_arguments)
	if (object == GLOBAL_PROC)
		return call(delegate)(arglist(calling_arguments))
	return call(object, delegate)(arglist(calling_arguments))

/**
  * Invoke this callback async (waitfor=false)
  * 
  * Calls the registered proc on the registered object, if the user ref
  * can be resolved it also inclues that as an arg
  *
  * If the datum being called on is varedited, the call is wrapped via WrapAdminProcCall
  */
/datum/callback/proc/InvokeAsync(...)
	set waitfor = FALSE

	if(!usr)
		var/datum/weakref/W = user
		if(W)
			var/mob/M = W.resolve()
			if(M)
				if (length(args))
					return world.PushUsr(arglist(list(M, src) + args))
				return world.PushUsr(M, src)

	if (!object)
		return

	var/list/calling_arguments = arguments
	if (length(args))
		if (length(arguments))
			calling_arguments = calling_arguments + args //not += so that it creates a new list so the arguments list stays clean
		else
			calling_arguments = args
	if(datum_flags & DF_VAR_EDITED)
		return WrapAdminProcCall(object, delegate, calling_arguments)
	if (object == GLOBAL_PROC)
		return call(delegate)(arglist(calling_arguments))
	return call(object, delegate)(arglist(calling_arguments))

/**
	Helper datum for the select callbacks proc
  */
/datum/callback_select
	var/list/finished
	var/pendingcount
	var/total

/datum/callback_select/New(count, savereturns)
	total = count
	if (savereturns)
		finished = new(count)


/datum/callback_select/proc/invoke_callback(index, datum/callback/callback, list/callback_args, savereturn = TRUE)
	set waitfor = FALSE
	if (!callback || !istype(callback))
		//This check only exists because the alternative is callback_select would block forever if given invalid data
		CRASH("invalid callback passed to invoke_callback")
	if (!length(callback_args))
		callback_args = list()
	pendingcount++
	var/rtn = callback.Invoke(arglist(callback_args))
	pendingcount--
	if (savereturn)
		finished[index] = rtn

/**
  * Runs a list of callbacks asyncronously, returning only when all have finished
  *
  * Callbacks can be repeated, to call it multiple times
  * 
  * Arguments:
  * * list/callbacks the list of callbacks to be called
  * * list/callback_args the list of lists of arguments to pass into each callback
  * * savereturns Optionally save and return the list of returned values from each of the callbacks
  * * resolution The number of byond ticks between each time you check if all callbacks are complete
  */
/proc/callback_select(list/callbacks, list/callback_args, savereturns = TRUE, resolution = 1)
	if (!callbacks)
		return
	var/count = length(callbacks)
	if (!count)
		return
	if (!callback_args)
		callback_args = list()

	callback_args.len = count

	var/datum/callback_select/CS = new(count, savereturns)
	for (var/i in 1 to count)
		CS.invoke_callback(i, callbacks[i], callback_args[i], savereturns)

	while(CS.pendingcount)
		sleep(resolution*world.tick_lag)
	return CS.finished

/proc/___callbacknew(typepath, arguments)
	new typepath(arglist(arguments))
