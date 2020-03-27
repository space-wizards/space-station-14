#define PAI_MISSING_SOFTWARE_MESSAGE "<span class='warning'>You must download the required software to use this.</span>"

/obj/screen/pai
	icon = 'icons/mob/screen_pai.dmi'
	var/required_software

/obj/screen/pai/Click()
	if(isobserver(usr) || usr.incapacitated())
		return FALSE
	var/mob/living/silicon/pai/pAI = usr
	if(required_software && !pAI.software.Find(required_software))
		to_chat(pAI, PAI_MISSING_SOFTWARE_MESSAGE)
		return FALSE
	return TRUE

/obj/screen/pai/software
	name = "Software Interface"
	icon_state = "pai"

/obj/screen/pai/software/Click()
	if(!..())
		return
	var/mob/living/silicon/pai/pAI = usr
	pAI.paiInterface()

/obj/screen/pai/shell
	name = "Toggle Holoform"
	icon_state = "pai_holoform"

/obj/screen/pai/shell/Click()
	if(!..())
		return
	var/mob/living/silicon/pai/pAI = usr
	if(pAI.holoform)
		pAI.fold_in(0)
	else
		pAI.fold_out()

/obj/screen/pai/chassis
	name = "Holochassis Appearance Composite"
	icon_state = "pai_chassis"

/obj/screen/pai/chassis/Click()
	if(!..())
		return
	var/mob/living/silicon/pai/pAI = usr
	pAI.choose_chassis()

/obj/screen/pai/rest
	name = "Rest"
	icon_state = "pai_rest"

/obj/screen/pai/rest/Click()
	if(!..())
		return
	var/mob/living/silicon/pai/pAI = usr
	pAI.lay_down()

/obj/screen/pai/light
	name = "Toggle Integrated Lights"
	icon_state = "light"

/obj/screen/pai/light/Click()
	if(!..())
		return
	var/mob/living/silicon/pai/pAI = usr
	pAI.toggle_integrated_light()

/obj/screen/pai/newscaster
	name = "pAI Newscaster"
	icon_state = "newscaster"
	required_software = "newscaster"

/obj/screen/pai/newscaster/Click()
	if(!..())
		return
	var/mob/living/silicon/pai/pAI = usr
	pAI.newscaster.ui_interact(usr)

/obj/screen/pai/host_monitor
	name = "Host Health Scan"
	icon_state = "host_monitor"
	required_software = "host scan"

/obj/screen/pai/host_monitor/Click()
	if(!..())
		return
	var/mob/living/silicon/pai/pAI = usr
	if(iscarbon(pAI.card.loc))
		pAI.hostscan.attack(pAI.card.loc, pAI)
	else
		to_chat(src, "<span class='warning'>You are not being carried by anyone!</span>")
		return 0

/obj/screen/pai/crew_manifest
	name = "Crew Manifest"
	icon_state = "manifest"
	required_software = "crew manifest"

/obj/screen/pai/crew_manifest/Click()
	if(!..())
		return
	var/mob/living/silicon/pai/pAI = usr
	pAI.ai_roster()

/obj/screen/pai/state_laws
	name = "State Laws"
	icon_state = "state_laws"

/obj/screen/pai/state_laws/Click()
	if(!..())
		return
	var/mob/living/silicon/pai/pAI = usr
	pAI.checklaws()

/obj/screen/pai/pda_msg_send
	name = "PDA - Send Message"
	icon_state = "pda_send"
	required_software = "digital messenger"

/obj/screen/pai/pda_msg_send/Click()
	if(!..())
		return
	var/mob/living/silicon/pai/pAI = usr
	pAI.cmd_send_pdamesg(usr)

/obj/screen/pai/pda_msg_show
	name = "PDA - Show Message Log"
	icon_state = "pda_receive"
	required_software = "digital messenger"

/obj/screen/pai/pda_msg_show/Click()
	if(!..())
		return
	var/mob/living/silicon/pai/pAI = usr
	pAI.cmd_show_message_log(usr)

/obj/screen/pai/image_take
	name = "Take Image"
	icon_state = "take_picture"
	required_software = "photography module"

/obj/screen/pai/image_take/Click()
	if(!..())
		return
	var/mob/living/silicon/pai/pAI = usr
	pAI.aicamera.toggle_camera_mode(usr)

/obj/screen/pai/image_view
	name = "View Images"
	icon_state = "view_images"
	required_software = "photography module"

/obj/screen/pai/image_view/Click()
	if(!..())
		return
	var/mob/living/silicon/pai/pAI = usr
	pAI.aicamera.viewpictures(usr)

/obj/screen/pai/radio
	name = "radio"
	icon = 'icons/mob/screen_cyborg.dmi'
	icon_state = "radio"

/obj/screen/pai/radio/Click()
	if(!..())
		return
	var/mob/living/silicon/pai/pAI = usr
	pAI.radio.interact(usr)

/datum/hud/pai/New(mob/living/silicon/pai/owner)
	..()
	var/obj/screen/using

// Software menu
	using = new /obj/screen/pai/software
	using.screen_loc = ui_pai_software
	static_inventory += using

// Holoform
	using = new /obj/screen/pai/shell
	using.screen_loc = ui_pai_shell
	static_inventory += using

// Chassis Select Menu
	using = new /obj/screen/pai/chassis
	using.screen_loc = ui_pai_chassis
	static_inventory += using

// Rest
	using = new /obj/screen/pai/rest
	using.screen_loc = ui_pai_rest
	static_inventory += using

// Integrated Light
	using = new /obj/screen/pai/light
	using.screen_loc = ui_pai_light
	static_inventory += using

// Newscaster
	using = new /obj/screen/pai/newscaster
	using.screen_loc = ui_pai_newscaster
	static_inventory += using

// Language menu
	using = new /obj/screen/language_menu
	using.screen_loc = ui_borg_language_menu
	static_inventory += using

// Host Monitor
	using = new /obj/screen/pai/host_monitor()
	using.screen_loc = ui_pai_host_monitor
	static_inventory += using

// Crew Manifest
	using = new /obj/screen/pai/crew_manifest()
	using.screen_loc = ui_pai_crew_manifest
	static_inventory += using

// Laws
	using = new /obj/screen/pai/state_laws()
	using.screen_loc = ui_pai_state_laws
	static_inventory += using

// PDA message
	using = new /obj/screen/pai/pda_msg_send()
	using.screen_loc = ui_pai_pda_send
	static_inventory += using

// PDA log
	using = new /obj/screen/pai/pda_msg_show()
	using.screen_loc = ui_pai_pda_log
	static_inventory += using

// Take image
	using = new /obj/screen/pai/image_take()
	using.screen_loc = ui_pai_take_picture
	static_inventory += using

// View images
	using = new /obj/screen/pai/image_view()
	using.screen_loc = ui_pai_view_images
	static_inventory += using

// Radio
	using = new /obj/screen/pai/radio()
	using.screen_loc = ui_borg_radio
	static_inventory += using

	update_software_buttons()

/datum/hud/pai/proc/update_software_buttons()
	var/mob/living/silicon/pai/owner = mymob
	for(var/obj/screen/pai/button in static_inventory)
		if(button.required_software)
			button.color = owner.software.Find(button.required_software) ? null : "#808080"

#undef PAI_MISSING_SOFTWARE_MESSAGE
