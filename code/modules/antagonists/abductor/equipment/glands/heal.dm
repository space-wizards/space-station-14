/obj/item/organ/heart/gland/heal
	true_name = "organic replicator"
	cooldown_low = 200
	cooldown_high = 400
	uses = -1
	human_only = TRUE
	icon_state = "health"
	mind_control_uses = 3
	mind_control_duration = 3000

/obj/item/organ/heart/gland/heal/activate()
	if(!(owner.mob_biotypes & MOB_ORGANIC))
		return

	for(var/organ in owner.internal_organs)
		if(istype(organ, /obj/item/organ/cyberimp))
			reject_implant(organ)
			return

	var/obj/item/organ/liver/liver = owner.getorganslot(ORGAN_SLOT_LIVER)
	if((!liver/* && !HAS_TRAIT(owner, TRAIT_NOMETABOLISM)*/) || (liver && ((liver.damage > (liver.maxHealth / 2)) || (istype(liver, /obj/item/organ/liver/cybernetic)))))
		replace_liver(liver)
		return

	var/obj/item/organ/lungs/lungs = owner.getorganslot(ORGAN_SLOT_LUNGS)
	if((!lungs && !HAS_TRAIT(owner, TRAIT_NOBREATH)) || (lungs && (istype(lungs, /obj/item/organ/lungs/cybernetic))))
		replace_lungs(lungs)
		return

	var/obj/item/organ/eyes/eyes = owner.getorganslot(ORGAN_SLOT_EYES)
	if(!eyes || (eyes && ((HAS_TRAIT_FROM(owner, TRAIT_NEARSIGHT, EYE_DAMAGE)) || (HAS_TRAIT_FROM(owner, TRAIT_BLIND, EYE_DAMAGE)) || (istype(eyes, /obj/item/organ/eyes/robotic)))))
		replace_eyes(eyes)
		return

	var/obj/item/bodypart/limb
	var/list/limb_list = list(BODY_ZONE_L_ARM, BODY_ZONE_R_ARM, BODY_ZONE_L_LEG, BODY_ZONE_R_LEG)
	for(var/zone in limb_list)
		limb = owner.get_bodypart(zone)
		if(!limb)
			replace_limb(zone)
			return
		if((limb.get_damage() >= (limb.max_damage / 2)) || (limb.status == BODYPART_ROBOTIC))
			replace_limb(zone, limb)
			return

	if(owner.getToxLoss() > 40)
		replace_blood()
		return
	var/tox_amount = 0
	for(var/datum/reagent/toxin/T in owner.reagents.reagent_list)
		tox_amount += owner.reagents.get_reagent_amount(T.type)
	if(tox_amount > 10)
		replace_blood()
		return
	if(owner.blood_volume < BLOOD_VOLUME_OKAY)
		owner.blood_volume = BLOOD_VOLUME_NORMAL
		to_chat(owner, "<span class='warning'>You feel your blood pulsing within you.</span>")
		return

	var/obj/item/bodypart/chest/chest = owner.get_bodypart(BODY_ZONE_CHEST)
	if((chest.get_damage() >= (chest.max_damage / 4)) || (chest.status == BODYPART_ROBOTIC))
		replace_chest(chest)
		return

/obj/item/organ/heart/gland/heal/proc/reject_implant(obj/item/organ/cyberimp/implant)
	owner.visible_message("<span class='warning'>[owner] vomits up his [implant.name]!</span>", "<span class='userdanger'>You suddenly vomit up your [implant.name]!</span>")
	owner.vomit(0, TRUE, TRUE, 1, FALSE, FALSE, FALSE, TRUE)
	implant.Remove(owner)
	implant.forceMove(owner.drop_location())

/obj/item/organ/heart/gland/heal/proc/replace_liver(obj/item/organ/liver/liver)
	if(liver)
		owner.visible_message("<span class='warning'>[owner] vomits up his [liver.name]!</span>", "<span class='userdanger'>You suddenly vomit up your [liver.name]!</span>")
		owner.vomit(0, TRUE, TRUE, 1, FALSE, FALSE, FALSE, TRUE)
		liver.Remove(owner)
		liver.forceMove(owner.drop_location())
	else
		to_chat(owner, "<span class='warning'>You feel a weird rumble in your bowels...</span>")

	var/liver_type = /obj/item/organ/liver
	if(owner?.dna?.species?.mutantliver)
		liver_type = owner.dna.species.mutantliver
	var/obj/item/organ/liver/new_liver = new liver_type()
	new_liver.Insert(owner)

/obj/item/organ/heart/gland/heal/proc/replace_lungs(obj/item/organ/lungs/lungs)
	if(lungs)
		owner.visible_message("<span class='warning'>[owner] vomits up his [lungs.name]!</span>", "<span class='userdanger'>You suddenly vomit up your [lungs.name]!</span>")
		owner.vomit(0, TRUE, TRUE, 1, FALSE, FALSE, FALSE, TRUE)
		lungs.Remove(owner)
		lungs.forceMove(owner.drop_location())
	else
		to_chat(owner, "<span class='warning'>You feel a weird rumble inside your chest...</span>")

	var/lung_type = /obj/item/organ/lungs
	if(owner.dna.species && owner.dna.species.mutantlungs)
		lung_type = owner.dna.species.mutantlungs
	var/obj/item/organ/lungs/new_lungs = new lung_type()
	new_lungs.Insert(owner)

/obj/item/organ/heart/gland/heal/proc/replace_eyes(obj/item/organ/eyes/eyes)
	if(eyes)
		owner.visible_message("<span class='warning'>[owner]'s [eyes.name] fall out of their sockets!</span>", "<span class='userdanger'>Your [eyes.name] fall out of their sockets!</span>")
		playsound(owner, 'sound/effects/splat.ogg', 50, TRUE)
		eyes.Remove(owner)
		eyes.forceMove(owner.drop_location())
	else
		to_chat(owner, "<span class='warning'>You feel a weird rumble behind your eye sockets...</span>")

	addtimer(CALLBACK(src, .proc/finish_replace_eyes), rand(100, 200))

/obj/item/organ/heart/gland/heal/proc/finish_replace_eyes()
	var/eye_type = /obj/item/organ/eyes
	if(owner.dna.species && owner.dna.species.mutanteyes)
		eye_type = owner.dna.species.mutanteyes
	var/obj/item/organ/eyes/new_eyes = new eye_type()
	new_eyes.Insert(owner)
	owner.visible_message("<span class='warning'>A pair of new eyes suddenly inflates into [owner]'s eye sockets!</span>", "<span class='userdanger'>A pair of new eyes suddenly inflates into your eye sockets!</span>")

/obj/item/organ/heart/gland/heal/proc/replace_limb(body_zone, obj/item/bodypart/limb)
	if(limb)
		owner.visible_message("<span class='warning'>[owner]'s [limb.name] suddenly detaches from [owner.p_their()] body!</span>", "<span class='userdanger'>Your [limb.name] suddenly detaches from your body!</span>")
		playsound(owner, "desceration", 50, TRUE, -1)
		limb.drop_limb()
	else
		to_chat(owner, "<span class='warning'>You feel a weird tingle in your [parse_zone(body_zone)]... even if you don't have one.</span>")

	addtimer(CALLBACK(src, .proc/finish_replace_limb, body_zone), rand(150, 300))

/obj/item/organ/heart/gland/heal/proc/finish_replace_limb(body_zone)
	owner.visible_message("<span class='warning'>With a loud snap, [owner]'s [parse_zone(body_zone)] rapidly grows back from [owner.p_their()] body!</span>",
	"<span class='userdanger'>With a loud snap, your [parse_zone(body_zone)] rapidly grows back from your body!</span>",
	"<span class='warning'>Your hear a loud snap.</span>")
	playsound(owner, 'sound/magic/demon_consume.ogg', 50, TRUE)
	owner.regenerate_limb(body_zone)

/obj/item/organ/heart/gland/heal/proc/replace_blood()
	owner.visible_message("<span class='warning'>[owner] starts vomiting huge amounts of blood!</span>", "<span class='userdanger'>You suddenly start vomiting huge amounts of blood!</span>")
	keep_replacing_blood()

/obj/item/organ/heart/gland/heal/proc/keep_replacing_blood()
	var/keep_going = FALSE
	owner.vomit(0, TRUE, FALSE, 3, FALSE, FALSE, FALSE, TRUE)
	owner.Stun(15)
	owner.adjustToxLoss(-15, TRUE, TRUE)

	owner.blood_volume = min(BLOOD_VOLUME_NORMAL, owner.blood_volume + 20)
	if(owner.blood_volume < BLOOD_VOLUME_NORMAL)
		keep_going = TRUE

	if(owner.getToxLoss())
		keep_going = TRUE
	for(var/datum/reagent/toxin/R in owner.reagents.reagent_list)
		owner.reagents.remove_reagent(R.type, 4)
		if(owner.reagents.has_reagent(R.type))
			keep_going = TRUE
	if(keep_going)
		addtimer(CALLBACK(src, .proc/keep_replacing_blood), 30)

/obj/item/organ/heart/gland/heal/proc/replace_chest(obj/item/bodypart/chest/chest)
	if(chest.status == BODYPART_ROBOTIC)
		owner.visible_message("<span class='warning'>[owner]'s [chest.name] rapidly expels its mechanical components, replacing them with flesh!</span>", "<span class='userdanger'>Your [chest.name] rapidly expels its mechanical components, replacing them with flesh!</span>")
		playsound(owner, 'sound/magic/clockwork/anima_fragment_attack.ogg', 50, TRUE)
		var/list/dirs = GLOB.alldirs.Copy()
		for(var/i in 1 to 3)
			var/obj/effect/decal/cleanable/robot_debris/debris = new(get_turf(owner))
			debris.streak(dirs)
	else
		owner.visible_message("<span class='warning'>[owner]'s [chest.name] sheds off its damaged flesh, rapidly replacing it!</span>", "<span class='warning'>Your [chest.name] sheds off its damaged flesh, rapidly replacing it!</span>")
		playsound(owner, 'sound/effects/splat.ogg', 50, TRUE)
		var/list/dirs = GLOB.alldirs.Copy()
		for(var/i in 1 to 3)
			var/obj/effect/decal/cleanable/blood/gibs/gibs = new(get_turf(owner))
			gibs.streak(dirs)

	var/obj/item/bodypart/chest/new_chest = new(null)
	new_chest.replace_limb(owner, TRUE)
	qdel(chest)
