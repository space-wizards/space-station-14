#define SDQL_qdel_datum(d) qdel(d)

//SDQL2 datumized, /tg/station special!

/*
	Welcome admins, badmins and coders alike, to Structured Datum Query Language.
	SDQL allows you to powerfully run code on batches of objects (or single objects, it's still unmatched
	even there.)
	When I say "powerfully" I mean it you're in for a ride.

	Ok so say you want to get a list of every mob. How does one do this?
	"SELECT /mob"
	This will open a list of every object in world that is a /mob.
	And you can VV them if you need.

	What if you want to get every mob on a *specific z-level*?
	"SELECT /mob WHERE z == 4"

	What if you want to select every mob on even numbered z-levels?
	"SELECT /mob WHERE z % 2 == 0"

	Can you see where this is going? You can select objects with an arbitrary expression.
	These expressions can also do variable access and proc calls (yes, both on-object and globals!)
	Keep reading!

	Ok. What if you want to get every machine in the SSmachine process list? Looping through world is kinda
	slow.

	"SELECT * IN SSmachines.machinery"

	Here "*" as type functions as a wildcard.
	We know everything in the global SSmachines.machinery list is a machine.

	You can specify "IN <expression>" to return a list to operate on.
	This can be any list that you can wizard together from global variables and global proc calls.
	Every variable/proc name in the "IN" block is global.
	It can also be a single object, in which case the object is wrapped in a list for you.
	So yeah SDQL is unironically better than VV for complex single-object operations.

	You can of course combine these.
	"SELECT * IN SSmachines.machinery WHERE z == 4"
	"SELECT * IN SSmachines.machinery WHERE stat & 2" // (2 is NOPOWER, can't use defines from SDQL. Sorry!)
	"SELECT * IN SSmachines.machinery WHERE stat & 2 && z == 4"

	The possibilities are endless (just don't crash the server, ok?).

	Oh it gets better.

	You can use "MAP <expression>" to run some code per object and use the result. For example:

	"SELECT /obj/machinery/power/smes MAP [charge / capacity * 100, RCon_tag, src]"

	This will give you a list of all the APCs, their charge AND RCon tag. Useful eh?

	[] being a list here. Yeah you can write out lists directly without > lol lists in VV. Color matrix
	shenanigans inbound.

	After the "MAP" segment is executed, the rest of the query executes as if it's THAT object you just made
	(here the list).
	Yeah, by the way, you can chain these MAP / WHERE things FOREVER!

	"SELECT /mob WHERE client MAP client WHERE holder MAP holder"

	You can also generate a new list on the fly using a selector array. @[] will generate a list of objects based off the selector provided.

	"SELECT /mob/living IN (@[/area/crew_quarters/bar MAP contents])[1]"

	What if some dumbass admin spawned a bajillion spiders and you need to kill them all?
	Oh yeah you'd rather not delete all the spiders in maintenace. Only that one room the spiders were
	spawned in.

	"DELETE /mob/living/carbon/superior_animal/giant_spider WHERE loc.loc == marked"

	Here I used VV to mark the area they were in, and since loc.loc = area, voila.
	Only the spiders in a specific area are gone.

	Or you know if you want to catch spiders that crawled into lockers too (how even?)

	"DELETE /mob/living/carbon/superior_animal/giant_spider WHERE global.get_area(src) == marked"

	What else can you do?

	Well suppose you'd rather gib those spiders instead of simply flat deleting them...

	"CALL gib() ON /mob/living/carbon/superior_animal/giant_spider WHERE global.get_area(src) == marked"

	Or you can have some fun..

	"CALL forceMove(marked) ON /mob/living/carbon/superior_animal"

	You can also run multiple queries sequentially:

	"CALL forceMove(marked) ON /mob/living/carbon/superior_animal; CALL gib() ON
	/mob/living/carbon/superior_animal"

	And finally, you can directly modify variables on objects.

	"UPDATE /mob WHERE client SET client.color = [0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0]"

	Don't crash the server, OK?

	"UPDATE /mob/living/carbon/monkey SET #null = forceMove(usr.loc)"

	Writing "#null" in front of the "=" will call the proc and discard the return value.

	A quick recommendation: before you run something like a DELETE or another query.. Run it through SELECT
	first.
	You'd rather not gib every player on accident.
	Or crash the server.

	By the way, queries are slow and take a while. Be patient.
	They don't hang the entire server though.

	With great power comes great responsability.

	Here's a slightly more formal quick reference.

	The 4 queries you can do are:

	"SELECT <selectors>"
	"CALL <proc call> ON <selectors>"
	"UPDATE <selectors> SET var=<value>,var2=<value>"
	"DELETE <selectors>"

	"<selectors>" in this context is "<type> [IN <source>] [chain of MAP/WHERE modifiers]"

	"IN" (or "FROM", that works too but it's kinda weird to read),
	is the list of objects to work on. This defaults to world if not provided.
	But doing something like "IN living_mob_list" is quite handy and can optimize your query.
	All names inside the IN block are global scope, so you can do living_mob_list (a global var) easily.
	You can also run it on a single object. Because SDQL is that convenient even for single operations.

	<type> filters out objects of, well, that type easily. "*" is a wildcard and just takes everything in
	the source list.

	And then there's the MAP/WHERE chain.
	These operate on each individual object being ran through the query.
	They're both expressions like IN, but unlike it the expression is scoped *on the object*.
	So if you do "WHERE z == 4", this does "src.z", effectively.
	If you want to access global variables, you can do `global.living_mob_list`.
	Same goes for procs.

	MAP "changes" the object into the result of the expression.
	WHERE "drops" the object if the expression is falsey (0, null or "")

	What can you do inside expressions?

	* Proc calls
	* Variable reads
	* Literals (numbers, strings, type paths, etc...)
	* \ref referencing: {0x30000cc} grabs the object with \ref [0x30000cc]
	* Lists: [a, b, c] or [a: b, c: d]
	* Math and stuff.
	* A few special variables: src (the object currently scoped on), usr (your mob),
		marked (your marked datum), global(global scope)

	TG ADDITIONS START:
	Add USING keyword to the front of the query to use options system
	The defaults aren't necessarily implemented, as there is no need to.
	Available options: (D) means default
	PROCCALL = (D)ASYNC, BLOCKING
	SELECT = FORCE_NULLS, (D)SKIP_NULLS
	PRIORITY = HIGH, (D) NORMAL
	AUTOGC = (D) AUTOGC, KEEP_ALIVE
	SEQUENTIAL = TRUE - The queries in this batch will be executed sequentially one by one not in parallel

	Example: USING PROCCALL = BLOCKING, SELECT = FORCE_NULLS, PRIORITY = HIGH SELECT /mob FROM world WHERE z == 1

*/


#define SDQL2_STATE_ERROR 0
#define SDQL2_STATE_IDLE 1
#define SDQL2_STATE_PRESEARCH 2
#define SDQL2_STATE_SEARCHING 3
#define SDQL2_STATE_EXECUTING 4
#define SDQL2_STATE_SWITCHING 5
#define SDQL2_STATE_HALTING 6

#define SDQL2_VALID_OPTION_TYPES list("proccall", "select", "priority", "autogc" , "sequential")
#define SDQL2_VALID_OPTION_VALUES list("async", "blocking", "force_nulls", "skip_nulls", "high", "normal", "keep_alive" , "true")

#define SDQL2_OPTION_SELECT_OUTPUT_SKIP_NULLS			(1<<0)
#define SDQL2_OPTION_BLOCKING_CALLS						(1<<1)
#define SDQL2_OPTION_HIGH_PRIORITY						(1<<2)		//High priority SDQL query, allow using almost all of the tick.
#define SDQL2_OPTION_DO_NOT_AUTOGC						(1<<3)
#define SDQL2_OPTION_SEQUENTIAL							(1<<4)

#define SDQL2_OPTIONS_DEFAULT		(SDQL2_OPTION_SELECT_OUTPUT_SKIP_NULLS)

#define SDQL2_IS_RUNNING (state == SDQL2_STATE_EXECUTING || state == SDQL2_STATE_SEARCHING || state == SDQL2_STATE_SWITCHING || state == SDQL2_STATE_PRESEARCH)
#define SDQL2_HALT_CHECK if(!SDQL2_IS_RUNNING) {state = SDQL2_STATE_HALTING; return FALSE;};

#define SDQL2_TICK_CHECK ((options & SDQL2_OPTION_HIGH_PRIORITY)? CHECK_TICK_HIGH_PRIORITY : CHECK_TICK)

#define SDQL2_STAGE_SWITCH_CHECK if(state != SDQL2_STATE_SWITCHING){\
		if(state == SDQL2_STATE_HALTING){\
			state = SDQL2_STATE_IDLE;\
			return FALSE}\
		state = SDQL2_STATE_ERROR;\
		CRASH("SDQL2 fatal error");};

/client/proc/SDQL2_query(query_text as message)
	set category = "Debug"
	if(!check_rights(R_DEBUG))  //Shouldn't happen... but just to be safe.
		message_admins("<span class='danger'>ERROR: Non-admin [key_name(usr)] attempted to execute a SDQL query!</span>")
		log_admin("Non-admin [key_name(usr)] attempted to execute a SDQL query!")
		return FALSE
	var/list/results = world.SDQL2_query(query_text, key_name_admin(usr), "[key_name(usr)]")
	if(length(results) == 3)
		for(var/I in 1 to 3)
			to_chat(usr, results[I])
	SSblackbox.record_feedback("nested tally", "SDQL query", 1, list(ckey, query_text))

/world/proc/SDQL2_query(query_text, log_entry1, log_entry2)
	var/query_log = "executed SDQL query(s): \"[query_text]\"."
	message_admins("[log_entry1] [query_log]")
	query_log = "[log_entry2] [query_log]"
	log_game(query_log)
	NOTICE(query_log)

	var/start_time_total = REALTIMEOFDAY
	var/sequential = FALSE

	if(!length(query_text))
		return
	var/list/query_list = SDQL2_tokenize(query_text)
	if(!length(query_list))
		return
	var/list/querys = SDQL_parse(query_list)
	if(!length(querys))
		return
	var/list/datum/SDQL2_query/running = list()
	var/list/datum/SDQL2_query/waiting_queue = list() //Sequential queries queue.

	for(var/list/query_tree in querys)
		var/datum/SDQL2_query/query = new /datum/SDQL2_query(query_tree)
		if(QDELETED(query))
			continue
		if(usr)
			query.show_next_to_key = usr.ckey
		waiting_queue += query
		if(query.options & SDQL2_OPTION_SEQUENTIAL)
			sequential = TRUE

	if(sequential) //Start first one
		var/datum/SDQL2_query/query = popleft(waiting_queue)
		running += query
		var/msg = "Starting query #[query.id] - [query.get_query_text()]."
		if(usr)
			to_chat(usr, "<span class='admin'>[msg]</span>")
		log_admin(msg)
		query.ARun()
	else //Start all
		for(var/datum/SDQL2_query/query in waiting_queue)
			running += query
			var/msg = "Starting query #[query.id] - [query.get_query_text()]."
			if(usr)
				to_chat(usr, "<span class='admin'>[msg]</span>")
			log_admin(msg)
			query.ARun()

	var/finished = FALSE
	var/objs_all = 0
	var/objs_eligible = 0
	var/selectors_used = FALSE
	var/list/combined_refs = list()
	do
		CHECK_TICK
		finished = TRUE
		for(var/i in running)
			var/datum/SDQL2_query/query = i
			if(QDELETED(query))
				running -= query
				continue
			else if(query.state != SDQL2_STATE_IDLE)
				finished = FALSE
				if(query.state == SDQL2_STATE_ERROR)
					if(usr)
						to_chat(usr, "<span class='admin'>SDQL query [query.get_query_text()] errored. It will NOT be automatically garbage collected. Please remove manually.</span>")
					running -= query
			else
				if(query.finished)
					objs_all += islist(query.obj_count_all)? length(query.obj_count_all) : query.obj_count_all
					objs_eligible += islist(query.obj_count_eligible)? length(query.obj_count_eligible) : query.obj_count_eligible
					selectors_used |= query.where_switched
					combined_refs |= query.select_refs
					running -= query
					if(!CHECK_BITFIELD(query.options, SDQL2_OPTION_DO_NOT_AUTOGC))
						QDEL_IN(query, 50)
					if(sequential && waiting_queue.len)
						finished = FALSE
						var/datum/SDQL2_query/next_query = popleft(waiting_queue)
						running += next_query
						var/msg = "Starting query #[next_query.id] - [next_query.get_query_text()]."
						if(usr)
							to_chat(usr, "<span class='admin'>[msg]</span>")
						log_admin(msg)
						next_query.ARun()
				else
					if(usr)
						to_chat(usr, "<span class='admin'>SDQL query [query.get_query_text()] was halted. It will NOT be automatically garbage collected. Please remove manually.</span>")
					running -= query
	while(!finished)

	var/end_time_total = REALTIMEOFDAY - start_time_total
	return list("<span class='admin'>SDQL query combined results: [query_text]</span>",\
		"<span class='admin'>SDQL query completed: [objs_all] objects selected by path, and [selectors_used ? objs_eligible : objs_all] objects executed on after WHERE filtering/MAPping if applicable.</span>",\
		"<span class='admin'>SDQL combined querys took [DisplayTimeText(end_time_total)] to complete.</span>") + combined_refs

GLOBAL_LIST_INIT(sdql2_queries, GLOB.sdql2_queries || list())
GLOBAL_DATUM_INIT(sdql2_vv_statobj, /obj/effect/statclick/SDQL2_VV_all, new(null, "VIEW VARIABLES (all)", null))

/datum/SDQL2_query
	var/list/query_tree
	var/state = SDQL2_STATE_IDLE
	var/options = SDQL2_OPTIONS_DEFAULT
	var/superuser = FALSE		//Run things like proccalls without using admin protections
	var/allow_admin_interact = TRUE		//Allow admins to do things to this excluding varedit these two vars
	var/static/id_assign = 1
	var/id = 0

	var/qdel_on_finish = FALSE

	//Last run
		//General
	var/finished = FALSE
	var/start_time
	var/end_time
	var/where_switched = FALSE
	var/show_next_to_key
		//Select query only
	var/list/select_refs
	var/list/select_text
		//Runtime tracked
			//These three are weird. For best performance, they are only a number when they're not being changed by the SDQL searching/execution code. They only become numbers when they finish changing.
	var/list/obj_count_all
	var/list/obj_count_eligible
	var/obj_count_finished

	//Statclick
	var/obj/effect/statclick/SDQL2_delete/delete_click
	var/obj/effect/statclick/SDQL2_action/action_click

/datum/SDQL2_query/New(list/tree, SU = FALSE, admin_interact = TRUE, _options = SDQL2_OPTIONS_DEFAULT, finished_qdel = FALSE)
	if(IsAdminAdvancedProcCall() || !LAZYLEN(tree))
		qdel(src)
		return
	LAZYADD(GLOB.sdql2_queries, src)
	superuser = SU
	allow_admin_interact = admin_interact
	query_tree = tree
	options = _options
	id = id_assign++
	qdel_on_finish = finished_qdel

/datum/SDQL2_query/Destroy()
	state = SDQL2_STATE_HALTING
	query_tree = null
	obj_count_all = null
	obj_count_eligible = null
	obj_count_finished = null
	select_text = null
	select_refs = null
	GLOB.sdql2_queries -= src
	return ..()

/datum/SDQL2_query/proc/get_query_text()
	var/list/out = list()
	recursive_list_print(out, query_tree)
	return out.Join()

/proc/recursive_list_print(list/output = list(), list/input, datum/callback/datum_handler, datum/callback/atom_handler)
	output += "\[ "
	for(var/i in 1 to input.len)
		var/final = i == input.len
		var/key = input[i]

		//print the key
		if(islist(key))
			recursive_list_print(output, key, datum_handler, atom_handler)
		else if(is_proper_datum(key) && (datum_handler || (isatom(key) && atom_handler)))
			if(isatom(key) && atom_handler)
				output += atom_handler.Invoke(key)
			else
				output += datum_handler.Invoke(key)
		else
			output += "[key]"

		//print the value
		var/is_value = (!isnum(key) && !isnull(input[key]))
		if(is_value)
			var/value = input[key]
			if(islist(value))
				recursive_list_print(output, value, datum_handler, atom_handler)
			else if(is_proper_datum(value) && (datum_handler || (isatom(value) && atom_handler)))
				if(isatom(value) && atom_handler)
					output += atom_handler.Invoke(value)
				else
					output += datum_handler.Invoke(value)
			else
				output += " = [value]"

		if(!final)
			output += " , "

	output += " \]"

/datum/SDQL2_query/proc/text_state()
	switch(state)
		if(SDQL2_STATE_ERROR)
			return "###ERROR"
		if(SDQL2_STATE_IDLE)
			return "####IDLE"
		if(SDQL2_STATE_PRESEARCH)
			return "PRESEARCH"
		if(SDQL2_STATE_SEARCHING)
			return "SEARCHING"
		if(SDQL2_STATE_EXECUTING)
			return "EXECUTING"
		if(SDQL2_STATE_SWITCHING)
			return "SWITCHING"
		if(SDQL2_STATE_HALTING)
			return "##HALTING"

/datum/SDQL2_query/proc/generate_stat()
	if(!allow_admin_interact)
		return
	if(!delete_click)
		delete_click = new(null, "INITIALIZING", src)
	if(!action_click)
		action_click = new(null, "INITIALIZNG", src)
	stat("[id]		", delete_click.update("DELETE QUERY | STATE : [text_state()] | ALL/ELIG/FIN \
	[islist(obj_count_all)? length(obj_count_all) : (isnull(obj_count_all)? "0" : obj_count_all)]/\
	[islist(obj_count_eligible)? length(obj_count_eligible) : (isnull(obj_count_eligible)? "0" : obj_count_eligible)]/\
	[islist(obj_count_finished)? length(obj_count_finished) : (isnull(obj_count_finished)? "0" : obj_count_finished)] - [get_query_text()]"))
	stat("			", action_click.update("[SDQL2_IS_RUNNING? "HALT" : "RUN"]"))

/datum/SDQL2_query/proc/delete_click()
	admin_del(usr)

/datum/SDQL2_query/proc/action_click()
	if(SDQL2_IS_RUNNING)
		admin_halt(usr)
	else
		admin_run(usr)

/datum/SDQL2_query/proc/admin_halt(user = usr)
	if(!SDQL2_IS_RUNNING)
		return
	var/msg = "[key_name(user)] has halted query #[id]"
	message_admins(msg)
	log_admin(msg)
	state = SDQL2_STATE_HALTING

/datum/SDQL2_query/proc/admin_run(mob/user = usr)
	if(SDQL2_IS_RUNNING)
		return
	var/msg = "[key_name(user)] has (re)started query #[id]"
	message_admins(msg)
	log_admin(msg)
	show_next_to_key = user.ckey
	ARun()

/datum/SDQL2_query/proc/admin_del(user = usr)
	var/msg = "[key_name(user)] has stopped + deleted query #[id]"
	message_admins(msg)
	log_admin(msg)
	qdel(src)

/datum/SDQL2_query/proc/set_option(name, value)
	switch(name)
		if("select")
			switch(value)
				if("force_nulls")
					DISABLE_BITFIELD(options, SDQL2_OPTION_SELECT_OUTPUT_SKIP_NULLS)
		if("proccall")
			switch(value)
				if("blocking")
					ENABLE_BITFIELD(options, SDQL2_OPTION_BLOCKING_CALLS)
		if("priority")
			switch(value)
				if("high")
					ENABLE_BITFIELD(options, SDQL2_OPTION_HIGH_PRIORITY)
		if("autogc")
			switch(value)
				if("keep_alive")
					ENABLE_BITFIELD(options, SDQL2_OPTION_DO_NOT_AUTOGC)
		if("sequential")
			switch(value)
				if("true")
					ENABLE_BITFIELD(options,SDQL2_OPTION_SEQUENTIAL)

/datum/SDQL2_query/proc/ARun()
	INVOKE_ASYNC(src, .proc/Run)

/datum/SDQL2_query/proc/Run()
	if(SDQL2_IS_RUNNING)
		return FALSE
	if(query_tree["options"])
		for(var/name in query_tree["options"])
			var/value = query_tree["options"][name]
			set_option(name, value)
	select_refs = list()
	select_text = null
	obj_count_all = 0
	obj_count_eligible = 0
	obj_count_finished = 0
	start_time = REALTIMEOFDAY

	state = SDQL2_STATE_PRESEARCH
	var/list/search_tree = PreSearch()
	SDQL2_STAGE_SWITCH_CHECK

	state = SDQL2_STATE_SEARCHING
	var/list/found = Search(search_tree)
	SDQL2_STAGE_SWITCH_CHECK

	state = SDQL2_STATE_EXECUTING
	Execute(found)
	SDQL2_STAGE_SWITCH_CHECK

	end_time = REALTIMEOFDAY
	state = SDQL2_STATE_IDLE
	finished = TRUE
	. = TRUE
	if(show_next_to_key)
		var/client/C = GLOB.directory[show_next_to_key]
		if(C)
			var/mob/showmob = C.mob
			to_chat(showmob, "<span class='admin'>SDQL query results: [get_query_text()]<br>\
			SDQL query completed: [islist(obj_count_all)? length(obj_count_all) : obj_count_all] objects selected by path, and \
			[where_switched? "[islist(obj_count_eligible)? length(obj_count_eligible) : obj_count_eligible] objects executed on after WHERE keyword selection." : ""]<br>\
			SDQL query took [DisplayTimeText(end_time - start_time)] to complete.</span>")
			if(length(select_text))
				var/text = islist(select_text)? select_text.Join() : select_text
				var/static/result_offset = 0
				showmob << browse(text, "window=SDQL-result-[result_offset++]")
	show_next_to_key = null
	if(qdel_on_finish)
		qdel(src)

/datum/SDQL2_query/proc/PreSearch()
	SDQL2_HALT_CHECK
	switch(query_tree[1])
		if("explain")
			SDQL_testout(query_tree["explain"])
			state = SDQL2_STATE_HALTING
			return
		if("call")
			. = query_tree["on"]
		if("select", "delete", "update")
			. = query_tree[query_tree[1]]
	state = SDQL2_STATE_SWITCHING

/datum/SDQL2_query/proc/Search(list/tree)
	SDQL2_HALT_CHECK
	var/type = tree[1]
	var/list/from = tree[2]
	var/list/objs = SDQL_from_objs(from)
	SDQL2_TICK_CHECK
	SDQL2_HALT_CHECK
	objs = SDQL_get_all(type, objs)
	SDQL2_TICK_CHECK
	SDQL2_HALT_CHECK

	// 1 and 2 are type and FROM.
	var/i = 3
	while (i <= tree.len)
		var/key = tree[i++]
		var/list/expression = tree[i++]
		switch (key)
			if ("map")
				for(var/j = 1 to objs.len)
					var/x = objs[j]
					objs[j] = SDQL_expression(x, expression)
					SDQL2_TICK_CHECK
					SDQL2_HALT_CHECK

			if ("where")
				where_switched = TRUE
				var/list/out = list()
				obj_count_eligible = out
				for(var/x in objs)
					if(SDQL_expression(x, expression))
						out += x
					SDQL2_TICK_CHECK
					SDQL2_HALT_CHECK
				objs = out
	if(islist(obj_count_eligible))
		obj_count_eligible = objs.len
	else
		obj_count_eligible = obj_count_all
	. = objs
	state = SDQL2_STATE_SWITCHING

/datum/SDQL2_query/proc/SDQL_from_objs(list/tree)
	if(IsAdminAdvancedProcCall())
		if("world" in tree)
			var/text = "[key_name(usr)] attempted to grab world with a procedure call to a SDQL datum."
			message_admins(text)
			log_admin(text)
			return
	if("world" in tree)
		return world
	return SDQL_expression(world, tree)

/datum/SDQL2_query/proc/SDQL_get_all(type, location)
	var/list/out = list()
	obj_count_all = out

// If only a single object got returned, wrap it into a list so the for loops run on it.
	if(!islist(location) && location != world)
		location = list(location)

	if(type == "*")
		for(var/i in location)
			var/datum/d = i
			if(d.can_vv_get() || superuser)
				out += d
			SDQL2_TICK_CHECK
			SDQL2_HALT_CHECK
		return out
	if(istext(type))
		type = text2path(type)
	var/typecache = typecacheof(type)

	if(ispath(type, /mob))
		for(var/mob/d in location)
			if(typecache[d.type] && (d.can_vv_get() || superuser))
				out += d
			SDQL2_TICK_CHECK
			SDQL2_HALT_CHECK

	else if(ispath(type, /turf))
		for(var/turf/d in location)
			if(typecache[d.type] && (d.can_vv_get() || superuser))
				out += d
			SDQL2_TICK_CHECK
			SDQL2_HALT_CHECK

	else if(ispath(type, /obj))
		for(var/obj/d in location)
			if(typecache[d.type] && (d.can_vv_get() || superuser))
				out += d
			SDQL2_TICK_CHECK
			SDQL2_HALT_CHECK

	else if(ispath(type, /area))
		for(var/area/d in location)
			if(typecache[d.type] && (d.can_vv_get() || superuser))
				out += d
			SDQL2_TICK_CHECK
			SDQL2_HALT_CHECK

	else if(ispath(type, /atom))
		for(var/atom/d in location)
			if(typecache[d.type] && (d.can_vv_get() || superuser))
				out += d
			SDQL2_TICK_CHECK
			SDQL2_HALT_CHECK

	else if(ispath(type, /datum))
		if(location == world) //snowflake for byond shortcut
			for(var/datum/d) //stupid byond trick to have it not return atoms to make this less laggy
				if(typecache[d.type] && (d.can_vv_get() || superuser))
					out += d
				SDQL2_TICK_CHECK
				SDQL2_HALT_CHECK
		else
			for(var/datum/d in location)
				if(typecache[d.type] && (d.can_vv_get() || superuser))
					out += d
				SDQL2_TICK_CHECK
				SDQL2_HALT_CHECK
	obj_count_all = out.len
	return out

/datum/SDQL2_query/proc/Execute(list/found)
	SDQL2_HALT_CHECK
	select_refs = list()
	select_text = list()
	switch(query_tree[1])
		if("call")
			for(var/i in found)
				if(!is_proper_datum(i))
					continue
				world.SDQL_var(i, query_tree["call"][1], null, i, superuser, src)
				obj_count_finished++
				SDQL2_TICK_CHECK
				SDQL2_HALT_CHECK

		if("delete")
			for(var/datum/d in found)
				SDQL_qdel_datum(d)
				obj_count_finished++
				SDQL2_TICK_CHECK
				SDQL2_HALT_CHECK

		if("select")
			var/list/text_list = list()
			var/print_nulls = !(options & SDQL2_OPTION_SELECT_OUTPUT_SKIP_NULLS)
			obj_count_finished = select_refs
			for(var/i in found)
				SDQL_print(i, text_list, print_nulls)
				select_refs[REF(i)] = TRUE
				SDQL2_TICK_CHECK
				SDQL2_HALT_CHECK
			select_text = text_list

		if("update")
			if("set" in query_tree)
				var/list/set_list = query_tree["set"]
				for(var/d in found)
					if(!is_proper_datum(d))
						continue
					SDQL_internal_vv(d, set_list)
					obj_count_finished++
					SDQL2_TICK_CHECK
					SDQL2_HALT_CHECK
	if(islist(obj_count_finished))
		obj_count_finished = length(obj_count_finished)
	state = SDQL2_STATE_SWITCHING

/datum/SDQL2_query/proc/SDQL_print(object, list/text_list, print_nulls = TRUE)
	if(is_proper_datum(object))
		text_list += "<A HREF='?_src_=vars;[HrefToken(TRUE)];Vars=[REF(object)]'>[REF(object)]</A> : [object]"
		if(istype(object, /atom))
			var/atom/A = object
			var/turf/T = A.loc
			var/area/a
			if(istype(T))
				text_list += " <font color='gray'>at</font> [T] [ADMIN_COORDJMP(T)]"
				a = T.loc
			else
				var/turf/final = get_turf(T)		//Recursive, hopefully?
				if(istype(final))
					text_list += " <font color='gray'>at</font> [final] [ADMIN_COORDJMP(final)]"
					a = final.loc
				else
					text_list += " <font color='gray'>at</font> nonexistant location"
			if(a)
				text_list += " <font color='gray'>in</font> area [a]"
				if(T.loc != a)
					text_list += " <font color='gray'>inside</font> [T]"
		text_list += "<br>"
	else if(islist(object))
		var/list/L = object
		var/first = TRUE
		text_list += "\["
		for (var/x in L)
			if (!first)
				text_list += ", "
			first = FALSE
			SDQL_print(x, text_list)
			if (!isnull(x) && !isnum(x) && L[x] != null)
				text_list += " -> "
				SDQL_print(L[L[x]])
		text_list += "]<br>"
	else
		if(isnull(object))
			if(print_nulls)
				text_list += "NULL<br>"
		else
			text_list += "[object]<br>"

/datum/SDQL2_query/CanProcCall()
	if(!allow_admin_interact)
		return FALSE
	return ..()

/datum/SDQL2_query/vv_edit_var(var_name, var_value)
	if(!allow_admin_interact)
		return FALSE
	if(var_name == NAMEOF(src, superuser) || var_name == NAMEOF(src, allow_admin_interact) || var_name == NAMEOF(src, query_tree))
		return FALSE
	return ..()

/datum/SDQL2_query/proc/SDQL_internal_vv(d, list/set_list)
	for(var/list/sets in set_list)
		var/datum/temp = d
		var/i = 0
		for(var/v in sets)
			if(v == "#null")
				SDQL_expression(d, set_list[sets])
				break
			if(++i == sets.len)
				if(superuser)
					if(temp.vars.Find(v))
						temp.vars[v] = SDQL_expression(d, set_list[sets])
				else
					temp.vv_edit_var(v, SDQL_expression(d, set_list[sets]))
				break
			if(temp.vars.Find(v) && (istype(temp.vars[v], /datum) || istype(temp.vars[v], /client)))
				temp = temp.vars[v]
			else
				break

/datum/SDQL2_query/proc/SDQL_function_blocking(datum/object, procname, list/arguments, source)
	var/list/new_args = list()
	for(var/arg in arguments)
		new_args[++new_args.len] = SDQL_expression(source, arg)
	if(object == GLOB) // Global proc.
		procname = "/proc/[procname]"
		return superuser? (call(procname)(new_args)) : (WrapAdminProcCall(GLOBAL_PROC, procname, new_args))
	return superuser? (call(object, procname)(new_args)) : (WrapAdminProcCall(object, procname, new_args))

/datum/SDQL2_query/proc/SDQL_function_async(datum/object, procname, list/arguments, source)
	set waitfor = FALSE
	return SDQL_function_blocking(object, procname, arguments, source)

/datum/SDQL2_query/proc/SDQL_expression(datum/object, list/expression, start = 1)
	var/result = 0
	var/val

	for(var/i = start, i <= expression.len, i++)
		var/op = ""

		if(i > start)
			op = expression[i]
			i++

		var/list/ret = SDQL_value(object, expression, i)
		val = ret["val"]
		i = ret["i"]

		if(op != "")
			switch(op)
				if("+")
					result = (result + val)
				if("-")
					result = (result - val)
				if("*")
					result = (result * val)
				if("/")
					result = (result / val)
				if("&")
					result = (result & val)
				if("|")
					result = (result | val)
				if("^")
					result = (result ^ val)
				if("%")
					result = (result % val)
				if("=", "==")
					result = (result == val)
				if("!=", "<>")
					result = (result != val)
				if("<")
					result = (result < val)
				if("<=")
					result = (result <= val)
				if(">")
					result = (result > val)
				if(">=")
					result = (result >= val)
				if("and", "&&")
					result = (result && val)
				if("or", "||")
					result = (result || val)
				else
					to_chat(usr, "<span class='danger'>SDQL2: Unknown op [op]</span>")
					result = null
		else
			result = val

	return result

/datum/SDQL2_query/proc/SDQL_value(datum/object, list/expression, start = 1)
	var/i = start
	var/val = null

	if(i > expression.len)
		return list("val" = null, "i" = i)

	if(istype(expression[i], /list))
		val = SDQL_expression(object, expression[i])

	else if(expression[i] == "TRUE")
		val = TRUE

	else if(expression[i] == "FALSE")
		val = FALSE

	else if(expression[i] == "!")
		var/list/ret = SDQL_value(object, expression, i + 1)
		val = !ret["val"]
		i = ret["i"]

	else if(expression[i] == "~")
		var/list/ret = SDQL_value(object, expression, i + 1)
		val = ~ret["val"]
		i = ret["i"]

	else if(expression[i] == "-")
		var/list/ret = SDQL_value(object, expression, i + 1)
		val = -ret["val"]
		i = ret["i"]

	else if(expression[i] == "null")
		val = null

	else if(isnum(expression[i]))
		val = expression[i]

	else if(ispath(expression[i]))
		val = expression[i]

	else if(expression[i][1] in list("'", "\""))
		val = copytext_char(expression[i], 2, -1)

	else if(expression[i] == "\[")
		var/list/expressions_list = expression[++i]
		val = list()
		for(var/list/expression_list in expressions_list)
			var/result = SDQL_expression(object, expression_list)
			var/assoc
			if(expressions_list[expression_list] != null)
				assoc = SDQL_expression(object, expressions_list[expression_list])
			if(assoc != null)
				// Need to insert the key like this to prevent duplicate keys fucking up.
				var/list/dummy = list()
				dummy[result] = assoc
				result = dummy
			val += result

	else if(expression[i] == "@\[")
		var/list/search_tree = expression[++i]
		var/already_searching = (state == SDQL2_STATE_SEARCHING) //In case we nest, don't want to break out of the searching state until we're all done.

		if(!already_searching)
			state = SDQL2_STATE_SEARCHING

		val = Search(search_tree)
		SDQL2_STAGE_SWITCH_CHECK

		if(!already_searching)
			state = SDQL2_STATE_EXECUTING
		else
			state = SDQL2_STATE_SEARCHING

	else
		val = world.SDQL_var(object, expression, i, object, superuser, src)
		i = expression.len

	return list("val" = val, "i" = i)

/proc/SDQL_parse(list/query_list)
	var/datum/SDQL_parser/parser = new()
	var/list/querys = list()
	var/list/query_tree = list()
	var/pos = 1
	var/querys_pos = 1
	var/do_parse = 0

	for(var/val in query_list)
		if(val == ";")
			do_parse = 1
		else if(pos >= query_list.len)
			query_tree += val
			do_parse = 1

		if(do_parse)
			parser.query = query_tree
			var/list/parsed_tree
			parsed_tree = parser.parse()
			if(parsed_tree.len > 0)
				querys.len = querys_pos
				querys[querys_pos] = parsed_tree
				querys_pos++
			else //There was an error so don't run anything, and tell the user which query has errored.
				to_chat(usr, "<span class='danger'>Parsing error on [querys_pos]\th query. Nothing was executed.</span>")
				return list()
			query_tree = list()
			do_parse = 0
		else
			query_tree += val
		pos++

	qdel(parser)
	return querys

/proc/SDQL_testout(list/query_tree, indent = 0)
	var/static/whitespace = "&nbsp;&nbsp;&nbsp; "
	var/spaces = ""
	for(var/s = 0, s < indent, s++)
		spaces += whitespace

	for(var/item in query_tree)
		if(istype(item, /list))
			to_chat(usr, "[spaces](")
			SDQL_testout(item, indent + 1)
			to_chat(usr, "[spaces])")

		else
			to_chat(usr, "[spaces][item]")

		if(!isnum(item) && query_tree[item])

			if(istype(query_tree[item], /list))
				to_chat(usr, "[spaces][whitespace](")
				SDQL_testout(query_tree[item], indent + 2)
				to_chat(usr, "[spaces][whitespace])")

			else
				to_chat(usr, "[spaces][whitespace][query_tree[item]]")

//Staying as a world proc as this is called too often for changes to offset the potential IsAdminAdvancedProcCall checking overhead.
/world/proc/SDQL_var(object, list/expression, start = 1, source, superuser, datum/SDQL2_query/query)
	var/v
	var/static/list/exclude = list("usr", "src", "marked", "global", "MC", "FS", "CFG")
	var/long = start < expression.len
	var/datum/D
	if(is_proper_datum(object))
		D = object

	if (object == world && (!long || expression[start + 1] == ".") && !(expression[start] in exclude) && copytext(expression[start], 1, 3) != "SS") //3 == length("SS") + 1
		to_chat(usr, "<span class='danger'>World variables are not allowed to be accessed. Use global.</span>")
		return null

	else if(expression [start] == "{" && long)
		if(lowertext(copytext(expression[start + 1], 1, 3)) != "0x") //3 == length("0x") + 1
			to_chat(usr, "<span class='danger'>Invalid pointer syntax: [expression[start + 1]]</span>")
			return null
		v = locate("\[[expression[start + 1]]]")
		if(!v)
			to_chat(usr, "<span class='danger'>Invalid pointer: [expression[start + 1]]</span>")
			return null
		start++
		long = start < expression.len
	else if(expression[start] == "(" && long)
		v = query.SDQL_expression(source, expression[start + 1])
		start++
		long = start < expression.len
	else if(D != null && (!long || expression[start + 1] == ".") && (expression[start] in D.vars))
		if(D.can_vv_get(expression[start]) || superuser)
			v = D.vars[expression[start]]
		else
			v = "SECRET"
	else if(D != null && long && expression[start + 1] == ":" && hascall(D, expression[start]))
		v = expression[start]
	else if(!long || expression[start + 1] == ".")
		switch(expression[start])
			if("usr")
				v = usr
			if("src")
				v = source
			if("marked")
				if(usr.client && usr.client.holder && usr.client.holder.marked_datum)
					v = usr.client.holder.marked_datum
				else
					return null
			if("world")
				v = world
			if("global")
				v = GLOB
			if("MC")
				v = Master
			if("FS")
				v = Failsafe
			if("CFG")
				v = config
			else
				if(copytext(expression[start], 1, 3) == "SS") //Subsystem //3 == length("SS") + 1
					var/SSname = copytext_char(expression[start], 3)
					var/SSlength = length(SSname)
					var/datum/controller/subsystem/SS
					var/SSmatch
					for(var/_SS in Master.subsystems)
						SS = _SS
						if(copytext("[SS.type]", -SSlength) == SSname)
							SSmatch = SS
							break
					if(!SSmatch)
						return null
					v = SSmatch
				else
					return null
	else if(object == GLOB) // Shitty ass hack kill me.
		v = expression[start]
	if(long)
		if(expression[start + 1] == ".")
			return SDQL_var(v, expression[start + 2], null, source, superuser, query)
		else if(expression[start + 1] == ":")
			return (query.options & SDQL2_OPTION_BLOCKING_CALLS)? query.SDQL_function_async(object, v, expression[start + 2], source) : query.SDQL_function_blocking(object, v, expression[start + 2], source)
		else if(expression[start + 1] == "\[" && islist(v))
			var/list/L = v
			var/index = query.SDQL_expression(source, expression[start + 2])
			if(isnum(index) && (!ISINTEGER(index) || L.len < index))
				to_chat(usr, "<span class='danger'>Invalid list index: [index]</span>")
				return null
			return L[index]
	return v

/proc/SDQL2_tokenize(query_text)

	var/list/whitespace = list(" ", "\n", "\t")
	var/list/single = list("(", ")", ",", "+", "-", ".", "\[", "]", "{", "}", ";", ":")
	var/list/multi = list(
					"=" = list("", "="),
					"<" = list("", "=", ">"),
					">" = list("", "="),
					"!" = list("", "="),
					"@" = list("\["))

	var/word = ""
	var/list/query_list = list()
	var/len = length(query_text)
	var/char = ""

	for(var/i = 1, i <= len, i += length(char))
		char = query_text[i]

		if(char in whitespace)
			if(word != "")
				query_list += word
				word = ""

		else if(char in single)
			if(word != "")
				query_list += word
				word = ""

			query_list += char

		else if(char in multi)
			if(word != "")
				query_list += word
				word = ""

			var/char2 = query_text[i + length(char)]

			if(char2 in multi[char])
				query_list += "[char][char2]"
				i++

			else
				query_list += char

		else if(char == "'")
			if(word != "")
				to_chat(usr, "\red SDQL2: You have an error in your SDQL syntax, unexpected ' in query: \"<font color=gray>[query_text]</font>\" following \"<font color=gray>[word]</font>\". Please check your syntax, and try again.")
				return null

			word = "'"

			for(i += length(char), i <= len, i += length(char))
				char = query_text[i]

				if(char == "'")
					if(query_text[i + length(char)] == "'")
						word += "'"
						i += length(query_text[i + length(char)])

					else
						break

				else
					word += char

			if(i > len)
				to_chat(usr, "\red SDQL2: You have an error in your SDQL syntax, unmatched ' in query: \"<font color=gray>[query_text]</font>\". Please check your syntax, and try again.")
				return null

			query_list += "[word]'"
			word = ""

		else if(char == "\"")
			if(word != "")
				to_chat(usr, "\red SDQL2: You have an error in your SDQL syntax, unexpected \" in query: \"<font color=gray>[query_text]</font>\" following \"<font color=gray>[word]</font>\". Please check your syntax, and try again.")
				return null

			word = "\""

			for(i += length(char), i <= len, i += length(char))
				char = query_text[i]

				if(char == "\"")
					if(query_text[i + length(char)] == "'")
						word += "\""
						i += length(query_text[i + length(char)])

					else
						break

				else
					word += char

			if(i > len)
				to_chat(usr, "\red SDQL2: You have an error in your SDQL syntax, unmatched \" in query: \"<font color=gray>[query_text]</font>\". Please check your syntax, and try again.")
				return null

			query_list += "[word]\""
			word = ""

		else
			word += char

	if(word != "")
		query_list += word
	return query_list

/proc/is_proper_datum(thing)
	return istype(thing, /datum) || istype(thing, /client)

/obj/effect/statclick/SDQL2_delete/Click()
	var/datum/SDQL2_query/Q = target
	Q.delete_click()

/obj/effect/statclick/SDQL2_action/Click()
	var/datum/SDQL2_query/Q = target
	Q.action_click()

/obj/effect/statclick/SDQL2_VV_all
	name = "VIEW VARIABLES"

/obj/effect/statclick/SDQL2_VV_all/Click()
	usr.client.debug_variables(GLOB.sdql2_queries)
