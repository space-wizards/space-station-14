/obj/item/organ/heart/gland/electric
	true_name = "electron accumulator/discharger"
	cooldown_low = 800
	cooldown_high = 1200
	icon_state = "species"
	uses = -1
	mind_control_uses = 2
	mind_control_duration = 900

/obj/item/organ/heart/gland/electric/Insert(mob/living/carbon/M, special = 0)
	..()
	ADD_TRAIT(owner, TRAIT_SHOCKIMMUNE, "abductor_gland")

/obj/item/organ/heart/gland/electric/Remove(mob/living/carbon/M, special = 0)
	REMOVE_TRAIT(owner, TRAIT_SHOCKIMMUNE, "abductor_gland")
	..()

/obj/item/organ/heart/gland/electric/activate()
	owner.visible_message("<span class='danger'>[owner]'s skin starts emitting electric arcs!</span>",\
	"<span class='warning'>You feel electric energy building up inside you!</span>")
	playsound(get_turf(owner), "sparks", 100, TRUE, -1)
	addtimer(CALLBACK(src, .proc/zap), rand(30, 100))

/obj/item/organ/heart/gland/electric/proc/zap()
	tesla_zap(owner, 4, 8000, ZAP_MOB_DAMAGE | ZAP_OBJ_DAMAGE | ZAP_MOB_STUN | ZAP_IS_TESLA)
	playsound(get_turf(owner), 'sound/magic/lightningshock.ogg', 50, TRUE)
