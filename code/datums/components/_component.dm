/**
  * # Component
  *
  * The component datum
  *
  * A component should be a single standalone unit
  * of functionality, that works by receiving signals from it's parent
  * object to provide some single functionality (i.e a slippery component)
  * that makes the object it's attached to cause people to slip over.
  * Useful when you want shared behaviour independent of type inheritance
  */
/datum/component
	/// Defines how duplicate existing components are handled when added to a datum
	/// See `COMPONENT_DUPE_*` definitions for available options
	var/dupe_mode = COMPONENT_DUPE_HIGHLANDER
	
	/// The type to check for duplication
	/// `null` means exact match on `type` (default)
	/// Any other type means that and all subtypes
	var/dupe_type
	
	/// The datum this components belongs to
	var/datum/parent
	
	/// Only set to true if you are able to properly transfer this component
	/// At a minimum RegisterWithParent and UnregisterFromParent should be used
	/// Make sure you also implement PostTransfer for any post transfer handling
	var/can_transfer = FALSE

/**
  * Create a new component.
  * Additional arguments are passed to `Initialize()`
  *
  * Arguments:
  * * datum/P the parent datum this component reacts to signals from
  */
/datum/component/New(datum/P, ...)
	parent = P
	var/list/arguments = args.Copy(2)
	if(Initialize(arglist(arguments)) == COMPONENT_INCOMPATIBLE)
		qdel(src, TRUE, TRUE)
		CRASH("Incompatible [type] assigned to a [P.type]! args: [json_encode(arguments)]")

	_JoinParent(P)

/**
  * Called during component creation with the same arguments as in new excluding parent.
  * Do not call `qdel(src)` from this function, `return COMPONENT_INCOMPATIBLE` instead
  */
/datum/component/proc/Initialize(...)
	return

/**
  * Properly removes the component from `parent` and cleans up references
  * Setting `force` makes it not check for and remove the component from the parent
  * Setting `silent` deletes the component without sending a `COMSIG_COMPONENT_REMOVING` signal
  */
/datum/component/Destroy(force=FALSE, silent=FALSE)
	if(!force && parent)
		_RemoveFromParent()
	if(!silent)
		SEND_SIGNAL(parent, COMSIG_COMPONENT_REMOVING, src)
	parent = null
	return ..()

/**
  * Internal proc to handle behaviour of components when joining a parent
  */
/datum/component/proc/_JoinParent()
	var/datum/P = parent
	//lazy init the parent's dc list
	var/list/dc = P.datum_components
	if(!dc)
		P.datum_components = dc = list()

	//set up the typecache
	var/our_type = type
	for(var/I in _GetInverseTypeList(our_type))
		var/test = dc[I]
		if(test)	//already another component of this type here
			var/list/components_of_type
			if(!length(test))
				components_of_type = list(test)
				dc[I] = components_of_type
			else
				components_of_type = test
			if(I == our_type)	//exact match, take priority
				var/inserted = FALSE
				for(var/J in 1 to components_of_type.len)
					var/datum/component/C = components_of_type[J]
					if(C.type != our_type) //but not over other exact matches
						components_of_type.Insert(J, I)
						inserted = TRUE
						break
				if(!inserted)
					components_of_type += src
			else	//indirect match, back of the line with ya
				components_of_type += src
		else	//only component of this type, no list
			dc[I] = src

	RegisterWithParent()

/**
  * Internal proc to handle behaviour when being removed from a parent
  */
/datum/component/proc/_RemoveFromParent()
	var/datum/P = parent
	var/list/dc = P.datum_components
	for(var/I in _GetInverseTypeList())
		var/list/components_of_type = dc[I]
		if(length(components_of_type))	//
			var/list/subtracted = components_of_type - src
			if(subtracted.len == 1)	//only 1 guy left
				dc[I] = subtracted[1]	//make him special
			else
				dc[I] = subtracted
		else	//just us
			dc -= I
	if(!dc.len)
		P.datum_components = null

	UnregisterFromParent()

/**
  * Register the component with the parent object
  *
  * Use this proc to register with your parent object
  * Overridable proc that's called when added to a new parent
  */
/datum/component/proc/RegisterWithParent()
	return

/**
  * Unregister from our parent object
  *
  * Use this proc to unregister from your parent object
  * Overridable proc that's called when removed from a parent
  * *
  */
/datum/component/proc/UnregisterFromParent()
	return

/**
  * Register to listen for a signal from the passed in target
  *
  * This sets up a listening relationship such that when the target object emits a signal
  * the source datum this proc is called upon, will recieve a callback to the given proctype
  * Return values from procs registered must be a bitfield
  *
  * Arguments:
  * * datum/target The target to listen for signals from
  * * sig_type_or_types Either a string signal name, or a list of signal names (strings)
  * * proctype The proc to call back when the signal is emitted
  * * override If a previous registration exists you must explicitly set this
  */
/datum/proc/RegisterSignal(datum/target, sig_type_or_types, proctype, override = FALSE)
	if(QDELETED(src) || QDELETED(target))
		return

	var/list/procs = signal_procs
	if(!procs)
		signal_procs = procs = list()
	if(!procs[target])
		procs[target] = list()
	var/list/lookup = target.comp_lookup
	if(!lookup)
		target.comp_lookup = lookup = list()

	var/list/sig_types = islist(sig_type_or_types) ? sig_type_or_types : list(sig_type_or_types)
	for(var/sig_type in sig_types)
		if(!override && procs[target][sig_type])
			stack_trace("[sig_type] overridden. Use override = TRUE to suppress this warning")

		procs[target][sig_type] = proctype

		if(!lookup[sig_type]) // Nothing has registered here yet
			lookup[sig_type] = src
		else if(lookup[sig_type] == src) // We already registered here
			continue
		else if(!length(lookup[sig_type])) // One other thing registered here
			lookup[sig_type] = list(lookup[sig_type]=TRUE)
			lookup[sig_type][src] = TRUE
		else // Many other things have registered here
			lookup[sig_type][src] = TRUE

	signal_enabled = TRUE

/**
  * Stop listening to a given signal from target
  *
  * Breaks the relationship between target and source datum, removing the callback when the signal fires
  * Doesn't care if a registration exists or not
  *
  * Arguments:
  * * datum/target Datum to stop listening to signals from
  * * sig_typeor_types Signal string key or list of signal keys to stop listening to specifically
  */
/datum/proc/UnregisterSignal(datum/target, sig_type_or_types)
	var/list/lookup = target.comp_lookup
	if(!signal_procs || !signal_procs[target] || !lookup)
		return
	if(!islist(sig_type_or_types))
		sig_type_or_types = list(sig_type_or_types)
	for(var/sig in sig_type_or_types)
		switch(length(lookup[sig]))
			if(2)
				lookup[sig] = (lookup[sig]-src)[1]
			if(1)
				stack_trace("[target] ([target.type]) somehow has single length list inside comp_lookup")
				if(src in lookup[sig])
					lookup -= sig
					if(!length(lookup))
						target.comp_lookup = null
						break
			if(0)
				lookup -= sig
				if(!length(lookup))
					target.comp_lookup = null
					break
			else
				lookup[sig] -= src

	signal_procs[target] -= sig_type_or_types
	if(!signal_procs[target].len)
		signal_procs -= target

/**
  * Called on a component when a component of the same type was added to the same parent
  * See `/datum/component/var/dupe_mode`
  * `C`'s type will always be the same of the called component
  */
/datum/component/proc/InheritComponent(datum/component/C, i_am_original)
	return


/**
  * Called on a component when a component of the same type was added to the same parent with COMPONENT_DUPE_SELECTIVE
  * See `/datum/component/var/dupe_mode`
  * `C`'s type will always be the same of the called component
  * return TRUE if you are absorbing the component, otherwise FALSE if you are fine having it exist as a duplicate component
  */
/datum/component/proc/CheckDupeComponent(datum/component/C, ...)
	return


/**
  * Callback Just before this component is transferred
  *
  * Use this to do any special cleanup you might need to do before being deregged from an object
  *
  */
/datum/component/proc/PreTransfer()
	return

/**
  * Callback Just after a component is transferred
  *
  * Use this to do any special setup you need to do after being moved to a new object
  * Do not call `qdel(src)` from this function, `return COMPONENT_INCOMPATIBLE` instead
  *
  */
/datum/component/proc/PostTransfer()
	return COMPONENT_INCOMPATIBLE //Do not support transfer by default as you must properly support it

/**
  * Internal proc to create a list of our type and all parent types
  */
/datum/component/proc/_GetInverseTypeList(our_type = type)
	//we can do this one simple trick
	var/current_type = parent_type
	. = list(our_type, current_type)
	//and since most components are root level + 1, this won't even have to run
	while (current_type != /datum/component)
		current_type = type2parent(current_type)
		. += current_type

/**
  * Internal proc to handle most all of the signaling procedure
  * Will runtime if used on datums with an empty component list
  * Use the `SEND_SIGNAL` define instead
  */
/datum/proc/_SendSignal(sigtype, list/arguments)
	var/target = comp_lookup[sigtype]
	if(!length(target))
		var/datum/C = target
		if(!C.signal_enabled)
			return NONE
		var/proctype = C.signal_procs[src][sigtype]
		return NONE | CallAsync(C, proctype, arguments)
	. = NONE
	for(var/I in target)
		var/datum/C = I
		if(!C.signal_enabled)
			continue
		var/proctype = C.signal_procs[src][sigtype]
		. |= CallAsync(C, proctype, arguments)

// The type arg is casted so initial works, you shouldn't be passing a real instance into this
/**
  * Return any component assigned to this datum of the given type
  * This will throw an error if it's possible to have more than one component of that type on the parent
  *
  * Arguments:
  * * datum/component/c_type The typepath of the component you want to get a reference to
  */
/datum/proc/GetComponent(datum/component/c_type)
	RETURN_TYPE(c_type)
	if(initial(c_type.dupe_mode) == COMPONENT_DUPE_ALLOWED || initial(c_type.dupe_mode) == COMPONENT_DUPE_SELECTIVE)
		stack_trace("GetComponent was called to get a component of which multiple copies could be on an object. This can easily break and should be changed. Type: \[[c_type]\]")
	var/list/dc = datum_components
	if(!dc)
		return null
	. = dc[c_type]
	if(length(.))
		return .[1]

// The type arg is casted so initial works, you shouldn't be passing a real instance into this
/**
  * Return any component assigned to this datum of the exact given type
  * This will throw an error if it's possible to have more than one component of that type on the parent
  *
  * Arguments:
  * * datum/component/c_type The typepath of the component you want to get a reference to
  */
/datum/proc/GetExactComponent(datum/component/c_type)
	RETURN_TYPE(c_type)
	if(initial(c_type.dupe_mode) == COMPONENT_DUPE_ALLOWED || initial(c_type.dupe_mode) == COMPONENT_DUPE_SELECTIVE)
		stack_trace("GetComponent was called to get a component of which multiple copies could be on an object. This can easily break and should be changed. Type: \[[c_type]\]")
	var/list/dc = datum_components
	if(!dc)
		return null
	var/datum/component/C = dc[c_type]
	if(C)
		if(length(C))
			C = C[1]
		if(C.type == c_type)
			return C
	return null

/**
  * Get all components of a given type that are attached to this datum
  *
  * Arguments:
  * * c_type The component type path
  */
/datum/proc/GetComponents(c_type)
	var/list/dc = datum_components
	if(!dc)
		return null
	. = dc[c_type]
	if(!length(.))
		return list(.)

/**
  * Creates an instance of `new_type` in the datum and attaches to it as parent
  * Sends the `COMSIG_COMPONENT_ADDED` signal to the datum
  * Returns the component that was created. Or the old component in a dupe situation where `COMPONENT_DUPE_UNIQUE` was set
  * If this tries to add an component to an incompatible type, the component will be deleted and the result will be `null`. This is very unperformant, try not to do it
  * Properly handles duplicate situations based on the `dupe_mode` var
  */
/datum/proc/AddComponent(new_type, ...)
	var/datum/component/nt = new_type
	var/dm = initial(nt.dupe_mode)
	var/dt = initial(nt.dupe_type)

	var/datum/component/old_comp
	var/datum/component/new_comp

	if(ispath(nt))
		if(nt == /datum/component)
			CRASH("[nt] attempted instantiation!")
	else
		new_comp = nt
		nt = new_comp.type

	args[1] = src

	if(dm != COMPONENT_DUPE_ALLOWED)
		if(!dt)
			old_comp = GetExactComponent(nt)
		else
			old_comp = GetComponent(dt)
		if(old_comp)
			switch(dm)
				if(COMPONENT_DUPE_UNIQUE)
					if(!new_comp)
						new_comp = new nt(arglist(args))
					if(!QDELETED(new_comp))
						old_comp.InheritComponent(new_comp, TRUE)
						QDEL_NULL(new_comp)
				if(COMPONENT_DUPE_HIGHLANDER)
					if(!new_comp)
						new_comp = new nt(arglist(args))
					if(!QDELETED(new_comp))
						new_comp.InheritComponent(old_comp, FALSE)
						QDEL_NULL(old_comp)
				if(COMPONENT_DUPE_UNIQUE_PASSARGS)
					if(!new_comp)
						var/list/arguments = args.Copy(2)
						old_comp.InheritComponent(null, TRUE, arguments)
					else
						old_comp.InheritComponent(new_comp, TRUE)
				if(COMPONENT_DUPE_SELECTIVE)
					var/list/arguments = args.Copy()
					arguments[1] = new_comp
					var/make_new_component = TRUE
					for(var/i in GetComponents(new_type))
						var/datum/component/C = i
						if(C.CheckDupeComponent(arglist(arguments)))
							make_new_component = FALSE
							QDEL_NULL(new_comp)
							break
					if(!new_comp && make_new_component)
						new_comp = new nt(arglist(args))
		else if(!new_comp)
			new_comp = new nt(arglist(args)) // There's a valid dupe mode but there's no old component, act like normal
	else if(!new_comp)
		new_comp = new nt(arglist(args)) // Dupes are allowed, act like normal

	if(!old_comp && !QDELETED(new_comp)) // Nothing related to duplicate components happened and the new component is healthy
		SEND_SIGNAL(src, COMSIG_COMPONENT_ADDED, new_comp)
		return new_comp
	return old_comp

/**
  * Get existing component of type, or create it and return a reference to it
  *
  * Use this if the item needs to exist at the time of this call, but may not have been created before now
  *
  * Arguments:
  * * component_type The typepath of the component to create or return
  * * ... additional arguments to be passed when creating the component if it does not exist
  */
/datum/proc/LoadComponent(component_type, ...)
	. = GetComponent(component_type)
	if(!.)
		return AddComponent(arglist(args))

/**
  * Removes the component from parent, ends up with a null parent
  */
/datum/component/proc/RemoveComponent()
	if(!parent)
		return
	var/datum/old_parent = parent
	PreTransfer()
	_RemoveFromParent()
	parent = null
	SEND_SIGNAL(old_parent, COMSIG_COMPONENT_REMOVING, src)

/**
  * Transfer this component to another parent
  *
  * Component is taken from source datum
  *
  * Arguments:
  * * datum/component/target Target datum to transfer to
  */
/datum/proc/TakeComponent(datum/component/target)
	if(!target || target.parent == src)
		return
	if(target.parent)
		target.RemoveComponent()
	target.parent = src
	var/result = target.PostTransfer()
	switch(result)
		if(COMPONENT_INCOMPATIBLE)
			var/c_type = target.type
			qdel(target)
			CRASH("Incompatible [c_type] transfer attempt to a [type]!")

	if(target == AddComponent(target))
		target._JoinParent()

/**
  * Transfer all components to target
  *
  * All components from source datum are taken
  *
  * Arguments:
  * * /datum/target the target to move the components to
  */
/datum/proc/TransferComponents(datum/target)
	var/list/dc = datum_components
	if(!dc)
		return
	var/comps = dc[/datum/component]
	if(islist(comps))
		for(var/datum/component/I in comps)
			if(I.can_transfer)
				target.TakeComponent(I)
	else
		var/datum/component/C = comps
		if(C.can_transfer)
			target.TakeComponent(comps)

/**
  * Return the object that is the host of any UI's that this component has
  */
/datum/component/ui_host()
	return parent
