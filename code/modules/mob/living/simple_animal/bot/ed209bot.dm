/mob/living/simple_animal/bot/secbot/ed209
	name = "\improper ED-209 Security Robot"
	desc = "A security robot. He looks less than thrilled."
	icon_state = "ed209"
	density = TRUE
	health = 100
	maxHealth = 100
	obj_damage = 60
	environment_smash = ENVIRONMENT_SMASH_WALLS //Walls can't stop THE LAW
	mob_size = MOB_SIZE_LARGE

	model = "ED-209"
	window_id = "autoed209"
	window_name = "Automatic Security Unit v2.6"
	ranged = TRUE
	var/lastfired = 0
	var/shot_delay = 15
	var/shoot_sound = 'sound/weapons/laser.ogg'
	var/projectile = /obj/projectile/beam/disabler
	var/fair_market_projectile = /obj/projectile/bullet/c38 // For shooting the worst scumbags of all: the poor

/mob/living/simple_animal/bot/secbot/ed209/Initialize(mapload)
	. = ..()
	set_weapon() //giving it the right projectile and firing sound.

/mob/living/simple_animal/bot/secbot/ed209/bot_reset()
	..()
	set_weapon()

/mob/living/simple_animal/bot/secbot/ed209/set_custom_texts()
	text_hack = "You disable [name]'s combat inhibitor."
	text_dehack = "You restore [name]'s combat inhibitor."
	text_dehack_fail = "[name] ignores your attempts to restrict him!"

/mob/living/simple_animal/bot/secbot/ed209/emag_act(mob/user)
	..()
	icon_state = "ed209[on]"
	set_weapon()

/mob/living/simple_animal/bot/secbot/ed209/handle_automated_action()
	var/judgement_criteria = judgement_criteria()
	var/list/targets = list()
	for(var/mob/living/carbon/C in view(7,src)) //Let's find us a target
		var/threatlevel = 0
		if(C.incapacitated())
			continue
		threatlevel = C.assess_threat(judgement_criteria, weaponcheck=CALLBACK(src, .proc/check_for_weapons))
		//speak(C.real_name + text(": threat: []", threatlevel))
		if(threatlevel < 4 )
			continue
		var/dst = get_dist(src, C)
		if(dst <= 1 || dst > 7)
			continue
		targets += C
	if(targets.len>0)
		var/mob/living/carbon/t = pick(targets)
		if(t.stat != DEAD && !t.handcuffed) //we don't shoot people who are dead, cuffed or lying down.
			shootAt(t)
	..()

/mob/living/simple_animal/bot/secbot/ed209/proc/set_weapon()  //used to update the projectile type and firing sound
	shoot_sound = 'sound/weapons/laser.ogg'
	if(emagged == 2)
		projectile = /obj/projectile/beam
	else
		projectile = /obj/projectile/beam/disabler

/mob/living/simple_animal/bot/secbot/ed209/proc/shootAt(mob/target)
	if(world.time <= lastfired + shot_delay)
		return
	lastfired = world.time
	var/turf/T = loc
	var/turf/U = get_turf(target)
	if(!U)
		return
	if(!isturf(T))
		return

	if(!projectile)
		return

	var/obj/projectile/A = new projectile (loc)
	playsound(src, shoot_sound, 50, TRUE)
	A.preparePixelProjectile(target, src)
	A.fire()

/mob/living/simple_animal/bot/secbot/ed209/emp_act(severity)
	if(severity == 2 && prob(70))
		severity = 1
	. = ..()
	if(. & EMP_PROTECT_SELF)
		return
	if (severity >= 2)
		new /obj/effect/temp_visual/emp(loc)
		var/list/mob/living/carbon/targets = new
		for(var/mob/living/carbon/C in view(12,src))
			if(C.stat==DEAD)
				continue
			targets += C
		if(targets.len)
			if(prob(50))
				var/mob/toshoot = pick(targets)
				if(toshoot)
					targets-=toshoot
					if(prob(50) && emagged < 2)
						emagged = 2
						set_weapon()
						shootAt(toshoot)
						emagged = FALSE
						set_weapon()
					else
						shootAt(toshoot)
			else if(prob(50))
				if(targets.len)
					var/mob/toarrest = pick(targets)
					if(toarrest)
						target = toarrest
						mode = BOT_HUNT

/mob/living/simple_animal/bot/secbot/ed209/RangedAttack(atom/A)
	if(!on)
		return
	shootAt(A)
