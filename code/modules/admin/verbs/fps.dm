//replaces the old Ticklag verb, fps is easier to understand
/client/proc/set_server_fps()
	set category = "Debug"
	set name = "Set Server FPS"
	set desc = "Sets game speed in frames-per-second. Can potentially break the game"

	if(!check_rights(R_DEBUG))
		return

	var/cfg_fps = CONFIG_GET(number/fps)
	var/new_fps = round(input("Sets game frames-per-second. Can potentially break the game (default: [cfg_fps])","FPS", world.fps) as num|null)

	if(new_fps <= 0)
		to_chat(src, "<span class='danger'>Error: set_server_fps(): Invalid world.fps value. No changes made.</span>")
		return
	if(new_fps > cfg_fps * 1.5)
		if(alert(src, "You are setting fps to a high value:\n\t[new_fps] frames-per-second\n\tconfig.fps = [cfg_fps]","Warning!","Confirm","ABORT-ABORT-ABORT") != "Confirm")
			return

	var/msg = "[key_name(src)] has modified world.fps to [new_fps]"
	log_admin(msg, 0)
	message_admins(msg, 0)
	SSblackbox.record_feedback("nested tally", "admin_toggle", 1, list("Set Server FPS", "[new_fps]")) //If you are copy-pasting this, ensure the 2nd parameter is unique to the new proc!

	CONFIG_SET(number/fps, new_fps)
	world.change_fps(new_fps)
