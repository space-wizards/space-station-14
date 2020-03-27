/obj/item/clothing/suit/space/space_ninja/proc/toggle_on_off()
	if(s_busy)
		to_chat(loc, "<span class='warning'>ERROR</span>: You cannot use this function at this time.")
		return FALSE
	if(s_initialized)
		deinitialize()
	else
		ninitialize()
	. = TRUE

/obj/item/clothing/suit/space/space_ninja/proc/ninitialize(delay = s_delay, mob/living/carbon/human/U = loc)
	if(!U.mind)
		return //Not sure how this could happen.
	s_busy = TRUE
	to_chat(U, "<span class='notice'>Now initializing...</span>")
	addtimer(CALLBACK(src, .proc/ninitialize_two, delay, U), delay)

/obj/item/clothing/suit/space/space_ninja/proc/ninitialize_two(delay, mob/living/carbon/human/U)
	if(!lock_suit(U))//To lock the suit onto wearer.
		s_busy = FALSE
		return
	to_chat(U, "<span class='notice'>Securing external locking mechanism...\nNeural-net established.</span>")
	addtimer(CALLBACK(src, .proc/ninitialize_three, delay, U), delay)

/obj/item/clothing/suit/space/space_ninja/proc/ninitialize_three(delay, mob/living/carbon/human/U)
	to_chat(U, "<span class='notice'>Extending neural-net interface...\nNow monitoring brain wave pattern...</span>")
	addtimer(CALLBACK(src, .proc/ninitialize_four, delay, U), delay)

/obj/item/clothing/suit/space/space_ninja/proc/ninitialize_four(delay, mob/living/carbon/human/U)
	if(U.stat == DEAD|| U.health <= 0)
		to_chat(U, "<span class='danger'><B>FÄAL ï¿½Rrï¿½R</B>: 344--93#ï¿½&&21 BRï¿½ï¿½N |/|/aVï¿½ PATT$RN <B>RED</B>\nA-A-aBï¿½rTï¿½NG...</span>")
		unlock_suit()
		s_busy = FALSE
		return
	lockIcons(U)//Check for icons.
	U.regenerate_icons()
	to_chat(U, "<span class='notice'>Linking neural-net interface...\nPattern</span>\green <B>GREEN</B><span class='notice'>, continuing operation.</span>")
	addtimer(CALLBACK(src, .proc/ninitialize_five, delay, U), delay)

/obj/item/clothing/suit/space/space_ninja/proc/ninitialize_five(delay, mob/living/carbon/human/U)
	to_chat(U, "<span class='notice'>VOID-shift device status: <B>ONLINE</B>.\nCLOAK-tech device status: <B>ONLINE</B>.</span>")
	addtimer(CALLBACK(src, .proc/ninitialize_six, delay, U), delay)

/obj/item/clothing/suit/space/space_ninja/proc/ninitialize_six(delay, mob/living/carbon/human/U)
	to_chat(U, "<span class='notice'>Primary system status: <B>ONLINE</B>.\nBackup system status: <B>ONLINE</B>.\nCurrent energy capacity: <B>[DisplayEnergy(cell.charge)]</B>.</span>")
	addtimer(CALLBACK(src, .proc/ninitialize_seven, delay, U), delay)

/obj/item/clothing/suit/space/space_ninja/proc/ninitialize_seven(delay, mob/living/carbon/human/U)
	to_chat(U, "<span class='notice'>All systems operational. Welcome to <B>SpiderOS</B>, [U.real_name].</span>")
	s_initialized = TRUE
	ntick()
	s_busy = FALSE



/obj/item/clothing/suit/space/space_ninja/proc/deinitialize(delay = s_delay)
	if(affecting==loc)
		var/mob/living/carbon/human/U = affecting
		if(alert("Are you certain you wish to remove the suit? This will take time and remove all abilities.",,"Yes","No")=="No")
			return
		s_busy = TRUE
		addtimer(CALLBACK(src, .proc/deinitialize_two, delay, U), delay)

/obj/item/clothing/suit/space/space_ninja/proc/deinitialize_two(delay, mob/living/carbon/human/U)
	to_chat(U, "<span class='notice'>Now de-initializing...</span>")
	addtimer(CALLBACK(src, .proc/deinitialize_three, delay, U), delay)

/obj/item/clothing/suit/space/space_ninja/proc/deinitialize_three(delay, mob/living/carbon/human/U)
	to_chat(U, "<span class='notice'>Logging off, [U.real_name]. Shutting down <B>SpiderOS</B>.</span>")
	addtimer(CALLBACK(src, .proc/deinitialize_four, delay, U), delay)

/obj/item/clothing/suit/space/space_ninja/proc/deinitialize_four(delay, mob/living/carbon/human/U)
	to_chat(U, "<span class='notice'>Primary system status: <B>OFFLINE</B>.\nBackup system status: <B>OFFLINE</B>.</span>")
	addtimer(CALLBACK(src, .proc/deinitialize_five, delay, U), delay)

/obj/item/clothing/suit/space/space_ninja/proc/deinitialize_five(delay, mob/living/carbon/human/U)
	to_chat(U, "<span class='notice'>VOID-shift device status: <B>OFFLINE</B>.\nCLOAK-tech device status: <B>OFFLINE</B>.</span>")
	cancel_stealth()//Shutdowns stealth.
	addtimer(CALLBACK(src, .proc/deinitialize_six, delay, U), delay)

/obj/item/clothing/suit/space/space_ninja/proc/deinitialize_six(delay, mob/living/carbon/human/U)
	to_chat(U, "<span class='notice'>Disconnecting neural-net interface...</span>\green<B>Success</B><span class='notice'>.</span>")
	addtimer(CALLBACK(src, .proc/deinitialize_seven, delay, U), delay)

/obj/item/clothing/suit/space/space_ninja/proc/deinitialize_seven(delay, mob/living/carbon/human/U)
	to_chat(U, "<span class='notice'>Disengaging neural-net interface...</span>\green<B>Success</B><span class='notice'>.</span>")
	addtimer(CALLBACK(src, .proc/deinitialize_eight, delay, U), delay)

/obj/item/clothing/suit/space/space_ninja/proc/deinitialize_eight(delay, mob/living/carbon/human/U)
	to_chat(U, "<span class='notice'>Unsecuring external locking mechanism...\nNeural-net abolished.\nOperation status: <B>FINISHED</B>.</span>")
	unlock_suit()
	U.regenerate_icons()
	s_initialized = FALSE
	s_busy = FALSE
