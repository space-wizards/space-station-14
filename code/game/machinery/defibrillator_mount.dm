//Holds defibs and recharges them from the powernet
//You can activate the mount with an empty hand to grab the paddles
//Not being adjacent will cause the paddles to snap back
/obj/machinery/defibrillator_mount
	name = "defibrillator mount"
	desc = "Holds and recharges defibrillators. You can grab the paddles if one is mounted."
	icon = 'icons/obj/machines/defib_mount.dmi'
	icon_state = "defibrillator_mount"
	density = FALSE
	use_power = IDLE_POWER_USE
	idle_power_usage = 1
	power_channel = EQUIP
	req_one_access = list(ACCESS_MEDICAL, ACCESS_HEADS, ACCESS_SECURITY) //used to control clamps
	var/obj/item/defibrillator/defib //this mount's defibrillator
	var/clamps_locked = FALSE //if true, and a defib is loaded, it can't be removed without unlocking the clamps

/obj/machinery/defibrillator_mount/loaded/Initialize() //loaded subtype for mapping use
	. = ..()
	defib = new/obj/item/defibrillator/loaded(src)

/obj/machinery/defibrillator_mount/Destroy()
	if(defib)
		QDEL_NULL(defib)
	. = ..()

/obj/machinery/defibrillator_mount/examine(mob/user)
	. = ..()
	if(defib)
		. += "<span class='notice'>There is a defib unit hooked up. Alt-click to remove it.</span>"
		if(GLOB.security_level >= SEC_LEVEL_RED)
			. += "<span class='notice'>Due to a security situation, its locking clamps can be toggled by swiping any ID.</span>"
		else
			. += "<span class='notice'>Its locking clamps can be [clamps_locked ? "dis" : ""]engaged by swiping an ID with access.</span>"

/obj/machinery/defibrillator_mount/process()
	if(defib && defib.cell && defib.cell.charge < defib.cell.maxcharge && is_operational())
		use_power(200)
		defib.cell.give(180) //90% efficiency, slightly better than the cell charger's 87.5%
		update_icon()

/obj/machinery/defibrillator_mount/update_overlays()
	. = ..()
	
	if(!defib)
		return
	
	. += "defib"
	
	if(defib.powered)
		. += (defib.safety ? "online" : "emagged")
		var/ratio = defib.cell.charge / defib.cell.maxcharge
		ratio = CEILING(ratio * 4, 1) * 25
		. += "charge[ratio]"
	
	if(clamps_locked)
		. += "clamps"

/obj/machinery/defibrillator_mount/get_cell()
	if(defib)
		return defib.get_cell()

//defib interaction
/obj/machinery/defibrillator_mount/attack_hand(mob/living/user)
	if(!defib)
		to_chat(user, "<span class='warning'>There's no defibrillator unit loaded!</span>")
		return
	if(defib.paddles.loc != defib)
		to_chat(user, "<span class='warning'>[defib.paddles.loc == user ? "You are already" : "Someone else is"] holding [defib]'s paddles!</span>")
		return
	user.put_in_hands(defib.paddles)

/obj/machinery/defibrillator_mount/attackby(obj/item/I, mob/living/user, params)
	if(istype(I, /obj/item/defibrillator))
		if(defib)
			to_chat(user, "<span class='warning'>There's already a defibrillator in [src]!</span>")
			return
		if(HAS_TRAIT(I, TRAIT_NODROP) || !user.transferItemToLoc(I, src))
			to_chat(user, "<span class='warning'>[I] is stuck to your hand!</span>")
			return
		user.visible_message("<span class='notice'>[user] hooks up [I] to [src]!</span>", \
		"<span class='notice'>You press [I] into the mount, and it clicks into place.</span>")
		playsound(src, 'sound/machines/click.ogg', 50, TRUE)
		defib = I
		update_icon()
		return
	else if(defib && I == defib.paddles)
		defib.paddles.snap_back()
		return
	var/obj/item/card/id = I.GetID()
	if(id)
		if(check_access(id) || GLOB.security_level >= SEC_LEVEL_RED) //anyone can toggle the clamps in red alert!
			if(!defib)
				to_chat(user, "<span class='warning'>You can't engage the clamps on a defibrillator that isn't there.</span>")
				return
			clamps_locked = !clamps_locked
			to_chat(user, "<span class='notice'>Clamps [clamps_locked ? "" : "dis"]engaged.</span>")
			update_icon()
		else
			to_chat(user, "<span class='warning'>Insufficient access.</span>")
		return
	..()

/obj/machinery/defibrillator_mount/multitool_act(mob/living/user, obj/item/multitool)
	..()
	if(!defib)
		to_chat(user, "<span class='warning'>There isn't any defibrillator to clamp in!</span>")
		return TRUE
	if(!clamps_locked)
		to_chat(user, "<span class='warning'>[src]'s clamps are disengaged!</span>")
		return TRUE
	user.visible_message("<span class='notice'>[user] presses [multitool] into [src]'s ID slot...</span>", \
	"<span class='notice'>You begin overriding the clamps on [src]...</span>")
	playsound(src, 'sound/machines/click.ogg', 50, TRUE)
	if(!do_after(user, 100, target = src) || !clamps_locked)
		return
	user.visible_message("<span class='notice'>[user] pulses [multitool], and [src]'s clamps slide up.</span>", \
	"<span class='notice'>You override the locking clamps on [src]!</span>")
	playsound(src, 'sound/machines/locktoggle.ogg', 50, TRUE)
	clamps_locked = FALSE
	update_icon()
	return TRUE

/obj/machinery/defibrillator_mount/AltClick(mob/living/carbon/user)
	if(!istype(user) || !user.canUseTopic(src, BE_CLOSE))
		return
	if(!defib)
		to_chat(user, "<span class='warning'>It'd be hard to remove a defib unit from a mount that has none.</span>")
		return
	if(clamps_locked)
		to_chat(user, "<span class='warning'>You try to tug out [defib], but the mount's clamps are locked tight!</span>")
		return
	if(!user.put_in_hands(defib))
		to_chat(user, "<span class='warning'>You need a free hand!</span>")
		return
	user.visible_message("<span class='notice'>[user] unhooks [defib] from [src].</span>", \
	"<span class='notice'>You slide out [defib] from [src] and unhook the charging cables.</span>")
	playsound(src, 'sound/items/deconstruct.ogg', 50, TRUE)
	defib = null
	update_icon()

//wallframe, for attaching the mounts easily
/obj/item/wallframe/defib_mount
	name = "unhooked defibrillator mount"
	desc = "A frame for a defibrillator mount. It can't be removed once it's placed."
	icon = 'icons/obj/machines/defib_mount.dmi'
	icon_state = "defibrillator_mount"
	custom_materials = list(/datum/material/iron = 300, /datum/material/glass = 100)
	w_class = WEIGHT_CLASS_BULKY
	result_path = /obj/machinery/defibrillator_mount
	pixel_shift = -28
