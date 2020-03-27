#define RAD_GEIGER_LOW 100							// Geiger counter sound thresholds
#define RAD_GEIGER_MEDIUM 500
#define RAD_GEIGER_HIGH 1000

/datum/looping_sound/geiger
	mid_sounds = list(
		list('sound/items/geiger/low1.ogg'=1, 'sound/items/geiger/low2.ogg'=1, 'sound/items/geiger/low3.ogg'=1, 'sound/items/geiger/low4.ogg'=1),
		list('sound/items/geiger/med1.ogg'=1, 'sound/items/geiger/med2.ogg'=1, 'sound/items/geiger/med3.ogg'=1, 'sound/items/geiger/med4.ogg'=1),
		list('sound/items/geiger/high1.ogg'=1, 'sound/items/geiger/high2.ogg'=1, 'sound/items/geiger/high3.ogg'=1, 'sound/items/geiger/high4.ogg'=1),
		list('sound/items/geiger/ext1.ogg'=1, 'sound/items/geiger/ext2.ogg'=1, 'sound/items/geiger/ext3.ogg'=1, 'sound/items/geiger/ext4.ogg'=1)
		)
	mid_length = 2
	volume = 25
	var/last_radiation

/datum/looping_sound/geiger/get_sound(starttime)
	var/danger
	switch(last_radiation)
		if(RAD_BACKGROUND_RADIATION to RAD_GEIGER_LOW)
			danger = 1
		if(RAD_GEIGER_LOW to RAD_GEIGER_MEDIUM)
			danger = 2
		if(RAD_GEIGER_MEDIUM to RAD_GEIGER_HIGH)
			danger = 3
		if(RAD_GEIGER_HIGH to INFINITY)
			danger = 4
		else
			return null
	return ..(starttime, mid_sounds[danger])

/datum/looping_sound/geiger/stop()
	. = ..()
	last_radiation = 0

#undef RAD_GEIGER_LOW
#undef RAD_GEIGER_MEDIUM
#undef RAD_GEIGER_HIGH

/datum/looping_sound/reverse_bear_trap
	mid_sounds = list('sound/effects/clock_tick.ogg')
	mid_length = 3.5
	volume = 25


/datum/looping_sound/reverse_bear_trap_beep
	mid_sounds = list('sound/machines/beep.ogg')
	mid_length = 60
	volume = 10
