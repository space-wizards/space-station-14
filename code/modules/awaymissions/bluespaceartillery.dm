

/obj/machinery/artillerycontrol
	var/reload = 60
	var/reload_cooldown = 60
	var/explosiondev = 3
	var/explosionmed = 6
	var/explosionlight = 12
	name = "bluespace artillery control"
	icon_state = "control_boxp1"
	icon = 'icons/obj/machines/particle_accelerator.dmi'
	density = TRUE

/obj/machinery/artillerycontrol/process()
	if(reload < reload_cooldown)
		reload++

/obj/structure/artilleryplaceholder
	name = "artillery"
	icon = 'icons/obj/machines/artillery.dmi'
	anchored = TRUE
	density = TRUE

/obj/structure/artilleryplaceholder/decorative
	density = FALSE

/obj/machinery/artillerycontrol/ui_interact(mob/user)
	. = ..()
	var/dat = "<B>Bluespace Artillery Control:</B><BR>"
	dat += "Locked on<BR>"
	dat += "<B>Charge progress: [reload]/[reload_cooldown]:</B><BR>"
	dat += "<A href='byond://?src=[REF(src)];fire=1'>Open Fire</A><BR>"
	dat += "Deployment of weapon authorized by <br>Nanotrasen Naval Command<br><br>Remember, friendly fire is grounds for termination of your contract and life.<HR>"
	user << browse(dat, "window=scroll")
	onclose(user, "scroll")

/obj/machinery/artillerycontrol/Topic(href, href_list)
	if(..())
		return
	var/A
	A = input("Area to bombard", "Open Fire", A) in GLOB.teleportlocs
	var/area/thearea = GLOB.teleportlocs[A]
	if(usr.stat || usr.restrained())
		return
	if(reload < reload_cooldown)
		return
	if(usr.contents.Find(src) || (in_range(src, usr) && isturf(loc)) || issilicon(usr))
		priority_announce("Bluespace artillery fire detected. Brace for impact.")
		message_admins("[ADMIN_LOOKUPFLW(usr)] has launched an artillery strike.")
		var/list/L = list()
		for(var/turf/T in get_area_turfs(thearea.type))
			L+=T
		var/loc = pick(L)
		explosion(loc,explosiondev,explosionmed,explosionlight)
		reload = 0
