#define BUCKET_LEN (world.fps*1*60) //how many ticks should we keep in the bucket. (1 minutes worth)
#define BUCKET_POS(timer) (((round((timer.timeToRun - SStimer.head_offset) / world.tick_lag)+1) % BUCKET_LEN)||BUCKET_LEN)
#define TIMER_MAX (world.time + TICKS2DS(min(BUCKET_LEN-(SStimer.practical_offset-DS2TICKS(world.time - SStimer.head_offset))-1, BUCKET_LEN-1)))
#define TIMER_ID_MAX (2**24) //max float with integer precision

SUBSYSTEM_DEF(timer)
	name = "Timer"
	wait = 1 //SS_TICKER subsystem, so wait is in ticks
	init_order = INIT_ORDER_TIMER

	flags = SS_TICKER|SS_NO_INIT

	var/list/datum/timedevent/second_queue = list() //awe, yes, you've had first queue, but what about second queue?
	var/list/hashes = list()

	var/head_offset = 0 //world.time of the first entry in the the bucket.
	var/practical_offset = 1 //index of the first non-empty item in the bucket.
	var/bucket_resolution = 0 //world.tick_lag the bucket was designed for
	var/bucket_count = 0 //how many timers are in the buckets

	var/list/bucket_list = list() //list of buckets, each bucket holds every timer that has to run that byond tick.

	var/list/timer_id_dict = list() //list of all active timers assoicated to their timer id (for easy lookup)

	var/list/clienttime_timers = list() //special snowflake timers that run on fancy pansy "client time"

	var/last_invoke_tick = 0
	var/static/last_invoke_warning = 0
	var/static/bucket_auto_reset = TRUE

/datum/controller/subsystem/timer/PreInit()
	bucket_list.len = BUCKET_LEN
	head_offset = world.time
	bucket_resolution = world.tick_lag

/datum/controller/subsystem/timer/stat_entry(msg)
	..("B:[bucket_count] P:[length(second_queue)] H:[length(hashes)] C:[length(clienttime_timers)] S:[length(timer_id_dict)]")

/datum/controller/subsystem/timer/fire(resumed = FALSE)
	var/lit = last_invoke_tick
	var/last_check = world.time - TICKS2DS(BUCKET_LEN*1.5)
	var/list/bucket_list = src.bucket_list

	if(!bucket_count)
		last_invoke_tick = world.time

	if(lit && lit < last_check && head_offset < last_check && last_invoke_warning < last_check)
		last_invoke_warning = world.time
		var/msg = "No regular timers processed in the last [BUCKET_LEN*1.5] ticks[bucket_auto_reset ? ", resetting buckets" : ""]!"
		message_admins(msg)
		WARNING(msg)
		if(bucket_auto_reset)
			bucket_resolution = 0

		log_world("Timer bucket reset. world.time: [world.time], head_offset: [head_offset], practical_offset: [practical_offset]")
		for (var/i in 1 to length(bucket_list))
			var/datum/timedevent/bucket_head = bucket_list[i]
			if (!bucket_head)
				continue

			log_world("Active timers at index [i]:")

			var/datum/timedevent/bucket_node = bucket_head
			var/anti_loop_check = 1000
			do
				log_world(get_timer_debug_string(bucket_node))
				bucket_node = bucket_node.next
				anti_loop_check--
			while(bucket_node && bucket_node != bucket_head && anti_loop_check)
		log_world("Active timers in the second_queue queue:")
		for(var/I in second_queue)
			log_world(get_timer_debug_string(I))

	var/next_clienttime_timer_index = 0
	var/len = length(clienttime_timers)

	for (next_clienttime_timer_index in 1 to len)
		if (MC_TICK_CHECK)
			next_clienttime_timer_index--
			break
		var/datum/timedevent/ctime_timer = clienttime_timers[next_clienttime_timer_index]
		if (ctime_timer.timeToRun > REALTIMEOFDAY)
			next_clienttime_timer_index--
			break

		var/datum/callback/callBack = ctime_timer.callBack
		if (!callBack)
			clienttime_timers.Cut(next_clienttime_timer_index,next_clienttime_timer_index+1)
			CRASH("Invalid timer: [get_timer_debug_string(ctime_timer)] world.time: [world.time], head_offset: [head_offset], practical_offset: [practical_offset], REALTIMEOFDAY: [REALTIMEOFDAY]")

		ctime_timer.spent = REALTIMEOFDAY
		callBack.InvokeAsync()

		if(ctime_timer.flags & TIMER_LOOP)
			ctime_timer.spent = 0
			ctime_timer.timeToRun = REALTIMEOFDAY + ctime_timer.wait
			BINARY_INSERT(ctime_timer, clienttime_timers, datum/timedevent, timeToRun)
		else
			qdel(ctime_timer)


	if (next_clienttime_timer_index)
		clienttime_timers.Cut(1, next_clienttime_timer_index+1)

	if (MC_TICK_CHECK)
		return

	var/static/list/spent = list()
	var/static/datum/timedevent/timer
	if (practical_offset > BUCKET_LEN)
		head_offset += TICKS2DS(BUCKET_LEN)
		practical_offset = 1
		resumed = FALSE

	if ((length(bucket_list) != BUCKET_LEN) || (world.tick_lag != bucket_resolution))
		reset_buckets()
		bucket_list = src.bucket_list
		resumed = FALSE


	if (!resumed)
		timer = null

	while (practical_offset <= BUCKET_LEN && head_offset + ((practical_offset-1)*world.tick_lag) <= world.time)
		var/datum/timedevent/head = bucket_list[practical_offset]
		if (!timer || !head || timer == head)
			head = bucket_list[practical_offset]
			timer = head
		while (timer)
			var/datum/callback/callBack = timer.callBack
			if (!callBack)
				bucket_resolution = null //force bucket recreation
				CRASH("Invalid timer: [get_timer_debug_string(timer)] world.time: [world.time], head_offset: [head_offset], practical_offset: [practical_offset]")

			if (!timer.spent)
				spent += timer
				timer.spent = world.time
				callBack.InvokeAsync()
				last_invoke_tick = world.time

			if (MC_TICK_CHECK)
				return

			timer = timer.next
			if (timer == head)
				break


		bucket_list[practical_offset++] = null

		//we freed up a bucket, lets see if anything in second_queue needs to be shifted to that bucket.
		var/i = 0
		var/L = length(second_queue)
		for (i in 1 to L)
			timer = second_queue[i]
			if (timer.timeToRun >= TIMER_MAX)
				i--
				break

			if (timer.timeToRun < head_offset)
				bucket_resolution = null //force bucket recreation
				stack_trace("[i] Invalid timer state: Timer in long run queue with a time to run less then head_offset. [get_timer_debug_string(timer)] world.time: [world.time], head_offset: [head_offset], practical_offset: [practical_offset]")

				if (timer.callBack && !timer.spent)
					timer.callBack.InvokeAsync()
					spent += timer
					bucket_count++
				else if(!QDELETED(timer))
					qdel(timer)
				continue

			if (timer.timeToRun < head_offset + TICKS2DS(practical_offset-1))
				bucket_resolution = null //force bucket recreation
				stack_trace("[i] Invalid timer state: Timer in long run queue that would require a backtrack to transfer to short run queue. [get_timer_debug_string(timer)] world.time: [world.time], head_offset: [head_offset], practical_offset: [practical_offset]")
				if (timer.callBack && !timer.spent)
					timer.callBack.InvokeAsync()
					spent += timer
					bucket_count++
				else if(!QDELETED(timer))
					qdel(timer)
				continue

			bucket_count++
			var/bucket_pos = max(1, BUCKET_POS(timer))

			var/datum/timedevent/bucket_head = bucket_list[bucket_pos]
			if (!bucket_head)
				bucket_list[bucket_pos] = timer
				timer.next = null
				timer.prev = null
				continue

			if (!bucket_head.prev)
				bucket_head.prev = bucket_head
			timer.next = bucket_head
			timer.prev = bucket_head.prev
			timer.next.prev = timer
			timer.prev.next = timer
		if (i)
			second_queue.Cut(1, i+1)

		timer = null

	bucket_count -= length(spent)

	for (var/i in spent)
		var/datum/timedevent/qtimer = i
		if(QDELETED(qtimer))
			bucket_count++
			continue
		if(!(qtimer.flags & TIMER_LOOP))
			qdel(qtimer)
		else
			bucket_count++
			qtimer.spent = 0
			qtimer.bucketEject()
			if(qtimer.flags & TIMER_CLIENT_TIME)
				qtimer.timeToRun = REALTIMEOFDAY + qtimer.wait
			else
				qtimer.timeToRun = world.time + qtimer.wait
			qtimer.bucketJoin()

	spent.len = 0

//formated this way to be runtime resistant
/datum/controller/subsystem/timer/proc/get_timer_debug_string(datum/timedevent/TE)
	. = "Timer: [TE]"
	. += "Prev: [TE.prev ? TE.prev : "NULL"], Next: [TE.next ? TE.next : "NULL"]"
	if(TE.spent)
		. += ", SPENT([TE.spent])"
	if(QDELETED(TE))
		. += ", QDELETED"
	if(!TE.callBack)
		. += ", NO CALLBACK"

/datum/controller/subsystem/timer/proc/reset_buckets()
	var/list/bucket_list = src.bucket_list
	var/list/alltimers = list()
	//collect the timers currently in the bucket
	for (var/bucket_head in bucket_list)
		if (!bucket_head)
			continue
		var/datum/timedevent/bucket_node = bucket_head
		do
			alltimers += bucket_node
			bucket_node = bucket_node.next
		while(bucket_node && bucket_node != bucket_head)

	bucket_list.len = 0
	bucket_list.len = BUCKET_LEN

	practical_offset = 1
	bucket_count = 0
	head_offset = world.time
	bucket_resolution = world.tick_lag

	alltimers += second_queue
	if (!length(alltimers))
		return

	sortTim(alltimers, .proc/cmp_timer)

	var/datum/timedevent/head = alltimers[1]

	if (head.timeToRun < head_offset)
		head_offset = head.timeToRun

	var/new_bucket_count
	var/i = 1
	for (i in 1 to length(alltimers))
		var/datum/timedevent/timer = alltimers[i]
		if (!timer)
			continue

		var/bucket_pos = BUCKET_POS(timer)
		if (timer.timeToRun >= TIMER_MAX)
			i--
			break


		if (!timer.callBack || timer.spent)
			WARNING("Invalid timer: [get_timer_debug_string(timer)] world.time: [world.time], head_offset: [head_offset], practical_offset: [practical_offset]")
			if (timer.callBack)
				qdel(timer)
			continue

		new_bucket_count++
		var/datum/timedevent/bucket_head = bucket_list[bucket_pos]
		if (!bucket_head)
			bucket_list[bucket_pos] = timer
			timer.next = null
			timer.prev = null
			continue

		if (!bucket_head.prev)
			bucket_head.prev = bucket_head
		timer.next = bucket_head
		timer.prev = bucket_head.prev
		timer.next.prev = timer
		timer.prev.next = timer
	if (i)
		alltimers.Cut(1, i+1)
	second_queue = alltimers
	bucket_count = new_bucket_count


/datum/controller/subsystem/timer/Recover()
	second_queue |= SStimer.second_queue
	hashes |= SStimer.hashes
	timer_id_dict |= SStimer.timer_id_dict
	bucket_list |= SStimer.bucket_list

/datum/timedevent
	var/id
	var/datum/callback/callBack
	var/timeToRun
	var/wait
	var/hash
	var/list/flags
	var/spent = 0 //time we ran the timer.
	var/name //for easy debugging.
	//cicular doublely linked list
	var/datum/timedevent/next
	var/datum/timedevent/prev

/datum/timedevent/New(datum/callback/callBack, wait, flags, hash)
	var/static/nextid = 1
	id = TIMER_ID_NULL
	src.callBack = callBack
	src.wait = wait
	src.flags = flags
	src.hash = hash

	if (flags & TIMER_CLIENT_TIME)
		timeToRun = REALTIMEOFDAY + wait
	else
		timeToRun = world.time + wait

	if (flags & TIMER_UNIQUE)
		SStimer.hashes[hash] = src

	if (flags & TIMER_STOPPABLE)
		id = num2text(nextid, 100)
		if (nextid >= SHORT_REAL_LIMIT)
			nextid += min(1, 2**round(nextid/SHORT_REAL_LIMIT))
		else
			nextid++
		SStimer.timer_id_dict[id] = src

	name = "Timer: [id] (\ref[src]), TTR: [timeToRun], Flags: [jointext(bitfield2list(flags, list("TIMER_UNIQUE", "TIMER_OVERRIDE", "TIMER_CLIENT_TIME", "TIMER_STOPPABLE", "TIMER_NO_HASH_WAIT", "TIMER_LOOP")), ", ")], callBack: \ref[callBack], callBack.object: [callBack.object]\ref[callBack.object]([getcallingtype()]), callBack.delegate:[callBack.delegate]([callBack.arguments ? callBack.arguments.Join(", ") : ""])"

	if ((timeToRun < world.time || timeToRun < SStimer.head_offset) && !(flags & TIMER_CLIENT_TIME))
		CRASH("Invalid timer state: Timer created that would require a backtrack to run (addtimer would never let this happen): [SStimer.get_timer_debug_string(src)]")

	if (callBack.object != GLOBAL_PROC && !QDESTROYING(callBack.object))
		LAZYADD(callBack.object.active_timers, src)

	bucketJoin()

/datum/timedevent/Destroy()
	..()
	if (flags & TIMER_UNIQUE && hash)
		SStimer.hashes -= hash

	if (callBack && callBack.object && callBack.object != GLOBAL_PROC && callBack.object.active_timers)
		callBack.object.active_timers -= src
		UNSETEMPTY(callBack.object.active_timers)

	callBack = null

	if (flags & TIMER_STOPPABLE)
		SStimer.timer_id_dict -= id

	if (flags & TIMER_CLIENT_TIME)
		if (!spent)
			spent = world.time
			SStimer.clienttime_timers -= src
		return QDEL_HINT_IWILLGC

	if (!spent)
		spent = world.time
		bucketEject()
	else
		if (prev && prev.next == src)
			prev.next = next
		if (next && next.prev == src)
			next.prev = prev
	next = null
	prev = null
	return QDEL_HINT_IWILLGC

/datum/timedevent/proc/bucketEject()
	var/bucketpos = BUCKET_POS(src)
	var/list/bucket_list = SStimer.bucket_list
	var/list/second_queue = SStimer.second_queue
	var/datum/timedevent/buckethead
	if(bucketpos > 0)
		buckethead = bucket_list[bucketpos]
	if(buckethead == src)
		bucket_list[bucketpos] = next
		SStimer.bucket_count--
	else if(timeToRun < TIMER_MAX || next || prev)
		SStimer.bucket_count--
	else
		var/l = length(second_queue)
		second_queue -= src
		if(l == length(second_queue))
			SStimer.bucket_count--
	if(prev != next)
		prev.next = next
		next.prev = prev
	else
		prev?.next = null
		next?.prev = null
	prev = next = null

/datum/timedevent/proc/bucketJoin()
	var/list/L

	if (flags & TIMER_CLIENT_TIME)
		L = SStimer.clienttime_timers
	else if (timeToRun >= TIMER_MAX)
		L = SStimer.second_queue

	if(L)
		BINARY_INSERT(src, L, datum/timedevent, timeToRun)
		return

	//get the list of buckets
	var/list/bucket_list = SStimer.bucket_list

	//calculate our place in the bucket list
	var/bucket_pos = BUCKET_POS(src)

	//get the bucket for our tick
	var/datum/timedevent/bucket_head = bucket_list[bucket_pos]
	SStimer.bucket_count++
	//empty bucket, we will just add ourselves
	if (!bucket_head)
		bucket_list[bucket_pos] = src
		return
	//other wise, lets do a simplified linked list add.
	if (!bucket_head.prev)
		bucket_head.prev = bucket_head
	next = bucket_head
	prev = bucket_head.prev
	next.prev = src
	prev.next = src

///Returns a string of the type of the callback for this timer
/datum/timedevent/proc/getcallingtype()
	. = "ERROR"
	if (callBack.object == GLOBAL_PROC)
		. = "GLOBAL_PROC"
	else
		. = "[callBack.object.type]"

/**
  * Create a new timer and insert it in the queue
  *
  * Arguments:
  * * callback the callback to call on timer finish
  * * wait deciseconds to run the timer for
  * * flags flags for this timer, see: code\__DEFINES\subsystems.dm
  */
/proc/addtimer(datum/callback/callback, wait = 0, flags = 0)
	if (!callback)
		CRASH("addtimer called without a callback")

	if (wait < 0)
		stack_trace("addtimer called with a negative wait. Converting to [world.tick_lag]")

	if (callback.object != GLOBAL_PROC && QDELETED(callback.object) && !QDESTROYING(callback.object))
		stack_trace("addtimer called with a callback assigned to a qdeleted object. In the future such timers will not be supported and may refuse to run or run with a 0 wait")

	wait = max(CEILING(wait, world.tick_lag), world.tick_lag)

	if(wait >= INFINITY)
		CRASH("Attempted to create timer with INFINITY delay")

	var/hash

	if (flags & TIMER_UNIQUE)
		var/list/hashlist
		if(flags & TIMER_NO_HASH_WAIT)
			hashlist = list(callback.object, "([REF(callback.object)])", callback.delegate, flags & TIMER_CLIENT_TIME)
		else
			hashlist = list(callback.object, "([REF(callback.object)])", callback.delegate, wait, flags & TIMER_CLIENT_TIME)
		hashlist += callback.arguments
		hash = hashlist.Join("|||||||")

		var/datum/timedevent/hash_timer = SStimer.hashes[hash]
		if(hash_timer)
			if (hash_timer.spent) //it's pending deletion, pretend it doesn't exist.
				hash_timer.hash = null //but keep it from accidentally deleting us
			else
				if (flags & TIMER_OVERRIDE)
					hash_timer.hash = null //no need having it delete it's hash if we are going to replace it
					qdel(hash_timer)
				else
					if (hash_timer.flags & TIMER_STOPPABLE)
						. = hash_timer.id
					return
	else if(flags & TIMER_OVERRIDE)
		stack_trace("TIMER_OVERRIDE used without TIMER_UNIQUE")

	var/datum/timedevent/timer = new(callback, wait, flags, hash)
	return timer.id

/**
  * Delete a timer
  *
  * Arguments:
  * * id a timerid or a /datum/timedevent
  */
/proc/deltimer(id)
	if (!id)
		return FALSE
	if (id == TIMER_ID_NULL)
		CRASH("Tried to delete a null timerid. Use TIMER_STOPPABLE flag")
	if (!istext(id))
		if (istype(id, /datum/timedevent))
			qdel(id)
			return TRUE
	//id is string
	var/datum/timedevent/timer = SStimer.timer_id_dict[id]
	if (timer && !timer.spent)
		qdel(timer)
		return TRUE
	return FALSE

/**
  * Get the remaining deciseconds on a timer
  *
  * Arguments:
  * * id a timerid or a /datum/timedevent
  */
/proc/timeleft(id)
	if (!id)
		return null
	if (id == TIMER_ID_NULL)
		CRASH("Tried to get timeleft of a null timerid. Use TIMER_STOPPABLE flag")
	if (!istext(id))
		if (istype(id, /datum/timedevent))
			var/datum/timedevent/timer = id
			return timer.timeToRun - world.time
	//id is string
	var/datum/timedevent/timer = SStimer.timer_id_dict[id]
	if (timer && !timer.spent)
		return timer.timeToRun - world.time
	return null

#undef BUCKET_LEN
#undef BUCKET_POS
#undef TIMER_MAX
#undef TIMER_ID_MAX
