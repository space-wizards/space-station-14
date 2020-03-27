/datum/config_entry/flag/autoadmin  // if autoadmin is enabled
	protection = CONFIG_ENTRY_LOCKED

/datum/config_entry/string/autoadmin_rank	// the rank for autoadmins
	config_entry_value = "Game Master"
	protection = CONFIG_ENTRY_LOCKED

/datum/config_entry/flag/auto_deadmin_players
	protection = CONFIG_ENTRY_LOCKED

/datum/config_entry/flag/auto_deadmin_antagonists
	protection = CONFIG_ENTRY_LOCKED

/datum/config_entry/flag/auto_deadmin_heads
	protection = CONFIG_ENTRY_LOCKED

/datum/config_entry/flag/auto_deadmin_silicons
	protection = CONFIG_ENTRY_LOCKED

/datum/config_entry/flag/auto_deadmin_security
	protection = CONFIG_ENTRY_LOCKED


/datum/config_entry/string/servername	// server name (the name of the game window)

/datum/config_entry/string/serversqlname	// short form server name used for the DB

/datum/config_entry/string/stationname	// station name (the name of the station in-game)

/datum/config_entry/number/lobby_countdown	// In between round countdown.
	config_entry_value = 120
	integer = FALSE
	min_val = 0

/datum/config_entry/number/round_end_countdown	// Post round murder death kill countdown
	config_entry_value = 25
	integer = FALSE
	min_val = 0

/datum/config_entry/flag/hub	// if the game appears on the hub or not

/datum/config_entry/flag/log_ooc	// log OOC channel

/datum/config_entry/flag/log_access	// log login/logout

/datum/config_entry/flag/log_say	// log client say

/datum/config_entry/flag/log_admin	// log admin actions
	protection = CONFIG_ENTRY_LOCKED

/datum/config_entry/flag/log_prayer	// log prayers

/datum/config_entry/flag/log_law	// log lawchanges

/datum/config_entry/flag/log_game	// log game events

/datum/config_entry/flag/log_mecha	// log mech data

/datum/config_entry/flag/log_virus	// log virology data

/datum/config_entry/flag/log_cloning // log cloning actions.

/datum/config_entry/flag/log_vote	// log voting

/datum/config_entry/flag/log_whisper	// log client whisper

/datum/config_entry/flag/log_attack	// log attack messages

/datum/config_entry/flag/log_emote	// log emotes

/datum/config_entry/flag/log_adminchat	// log admin chat messages
	protection = CONFIG_ENTRY_LOCKED

/datum/config_entry/flag/log_pda	// log pda messages

/datum/config_entry/flag/log_telecomms	// log telecomms messages

/datum/config_entry/flag/log_twitter	// log certain expliotable parrots and other such fun things in a JSON file of twitter valid phrases.

/datum/config_entry/flag/log_world_topic	// log all world.Topic() calls

/datum/config_entry/flag/log_manifest	// log crew manifest to seperate file

/datum/config_entry/flag/log_job_debug	// log roundstart divide occupations debug information to a file

/datum/config_entry/flag/log_shuttle // log shuttle related actions, ie shuttle computers, shuttle manipulator, emergency console

/datum/config_entry/flag/allow_admin_ooccolor	// Allows admins with relevant permissions to have their own ooc colour

/datum/config_entry/flag/allow_admin_asaycolor //Allows admins with relevant permissions to have a personalized asay color

/datum/config_entry/flag/allow_vote_restart	// allow votes to restart

/datum/config_entry/flag/allow_vote_mode	// allow votes to change mode

/datum/config_entry/flag/allow_vote_map	// allow votes to change map

/datum/config_entry/number/vote_delay	// minimum time between voting sessions (deciseconds, 10 minute default)
	config_entry_value = 6000
	integer = FALSE
	min_val = 0

/datum/config_entry/number/vote_period  // length of voting period (deciseconds, default 1 minute)
	config_entry_value = 600
	integer = FALSE
	min_val = 0

/datum/config_entry/flag/default_no_vote	// vote does not default to nochange/norestart

/datum/config_entry/flag/no_dead_vote	// dead people can't vote

/datum/config_entry/flag/allow_metadata	// Metadata is supported.

/datum/config_entry/flag/popup_admin_pm	// adminPMs to non-admins show in a pop-up 'reply' window when set

/datum/config_entry/number/fps
	config_entry_value = 20
	integer = FALSE
	min_val = 1
	max_val = 100   //byond will start crapping out at 50, so this is just ridic
	var/sync_validate = FALSE

/datum/config_entry/number/fps/ValidateAndSet(str_val)
	. = ..()
	if(.)
		sync_validate = TRUE
		var/datum/config_entry/number/ticklag/TL = config.entries_by_type[/datum/config_entry/number/ticklag]
		if(!TL.sync_validate)
			TL.ValidateAndSet(10 / config_entry_value)
		sync_validate = FALSE

/datum/config_entry/number/ticklag
	integer = FALSE
	var/sync_validate = FALSE

/datum/config_entry/number/ticklag/New()	//ticklag weirdly just mirrors fps
	var/datum/config_entry/CE = /datum/config_entry/number/fps
	config_entry_value = 10 / initial(CE.config_entry_value)
	..()

/datum/config_entry/number/ticklag/ValidateAndSet(str_val)
	. = text2num(str_val) > 0 && ..()
	if(.)
		sync_validate = TRUE
		var/datum/config_entry/number/fps/FPS = config.entries_by_type[/datum/config_entry/number/fps]
		if(!FPS.sync_validate)
			FPS.ValidateAndSet(10 / config_entry_value)
		sync_validate = FALSE

/datum/config_entry/flag/allow_holidays

/datum/config_entry/number/tick_limit_mc_init	//SSinitialization throttling
	config_entry_value = TICK_LIMIT_MC_INIT_DEFAULT
	min_val = 0 //oranges warned us
	integer = FALSE

/datum/config_entry/flag/admin_legacy_system	//Defines whether the server uses the legacy admin system with admins.txt or the SQL system
	protection = CONFIG_ENTRY_LOCKED

/datum/config_entry/flag/protect_legacy_admins	//Stops any admins loaded by the legacy system from having their rank edited by the permissions panel
	protection = CONFIG_ENTRY_LOCKED

/datum/config_entry/flag/protect_legacy_ranks	//Stops any ranks loaded by the legacy system from having their flags edited by the permissions panel
	protection = CONFIG_ENTRY_LOCKED

/datum/config_entry/flag/enable_localhost_rank	//Gives the !localhost! rank to any client connecting from 127.0.0.1 or ::1
	protection = CONFIG_ENTRY_LOCKED

/datum/config_entry/flag/load_legacy_ranks_only	//Loads admin ranks only from legacy admin_ranks.txt, while enabled ranks are mirrored to the database
	protection = CONFIG_ENTRY_LOCKED

/datum/config_entry/string/hostedby

/datum/config_entry/flag/norespawn

/datum/config_entry/flag/guest_jobban

/datum/config_entry/flag/usewhitelist

/datum/config_entry/flag/use_age_restriction_for_jobs	//Do jobs use account age restrictions? --requires database

/datum/config_entry/flag/use_account_age_for_jobs	//Uses the time they made the account for the job restriction stuff. New player joining alerts should be unaffected.

/datum/config_entry/flag/use_exp_tracking

/datum/config_entry/flag/use_exp_restrictions_heads

/datum/config_entry/number/use_exp_restrictions_heads_hours
	config_entry_value = 0
	integer = FALSE
	min_val = 0

/datum/config_entry/flag/use_exp_restrictions_heads_department

/datum/config_entry/flag/use_exp_restrictions_other

/datum/config_entry/flag/use_exp_restrictions_admin_bypass

/datum/config_entry/string/server

/datum/config_entry/string/banappeals

/datum/config_entry/string/wikiurl
	config_entry_value = "http://www.tgstation13.org/wiki"

/datum/config_entry/string/forumurl
	config_entry_value = "http://tgstation13.org/phpBB/index.php"

/datum/config_entry/string/rulesurl
	config_entry_value = "http://www.tgstation13.org/wiki/Rules"

/datum/config_entry/string/githuburl
	config_entry_value = "https://www.github.com/tgstation/-tg-station"

/datum/config_entry/string/roundstatsurl

/datum/config_entry/string/gamelogurl

/datum/config_entry/number/githubrepoid
	config_entry_value = null
	min_val = 0

/datum/config_entry/flag/guest_ban

/datum/config_entry/number/id_console_jobslot_delay
	config_entry_value = 30
	integer = FALSE
	min_val = 0

/datum/config_entry/number/inactivity_period	//time in ds until a player is considered inactive
	config_entry_value = 3000
	integer = FALSE
	min_val = 0

/datum/config_entry/number/inactivity_period/ValidateAndSet(str_val)
	. = ..()
	if(.)
		config_entry_value *= 10 //documented as seconds in config.txt

/datum/config_entry/number/afk_period	//time in ds until a player is considered inactive
	config_entry_value = 3000
	integer = FALSE
	min_val = 0

/datum/config_entry/number/afk_period/ValidateAndSet(str_val)
	. = ..()
	if(.)
		config_entry_value *= 10 //documented as seconds in config.txt

/datum/config_entry/flag/kick_inactive	//force disconnect for inactive players

/datum/config_entry/flag/load_jobs_from_txt

/datum/config_entry/flag/forbid_singulo_possession

/datum/config_entry/flag/automute_on	//enables automuting/spam prevention

/datum/config_entry/string/panic_server_name

/datum/config_entry/string/panic_server_name/ValidateAndSet(str_val)
	return str_val != "\[Put the name here\]" && ..()

/datum/config_entry/string/panic_server_address	//Reconnect a player this linked server if this server isn't accepting new players

/datum/config_entry/string/panic_server_address/ValidateAndSet(str_val)
	return str_val != "byond://address:port" && ..()

/datum/config_entry/string/invoke_youtubedl
	protection = CONFIG_ENTRY_LOCKED | CONFIG_ENTRY_HIDDEN

/datum/config_entry/flag/show_irc_name

/datum/config_entry/flag/see_own_notes	//Can players see their own admin notes

/datum/config_entry/number/note_fresh_days
	config_entry_value = null
	min_val = 0
	integer = FALSE

/datum/config_entry/number/note_stale_days
	config_entry_value = null
	min_val = 0
	integer = FALSE

/datum/config_entry/flag/maprotation

/datum/config_entry/number/maprotatechancedelta
	config_entry_value = 0.75
	min_val = 0
	max_val = 1
	integer = FALSE

/datum/config_entry/number/soft_popcap
	config_entry_value = null
	min_val = 0

/datum/config_entry/number/hard_popcap
	config_entry_value = null
	min_val = 0

/datum/config_entry/number/extreme_popcap
	config_entry_value = null
	min_val = 0

/datum/config_entry/string/soft_popcap_message
	config_entry_value = "Be warned that the server is currently serving a high number of users, consider using alternative game servers."

/datum/config_entry/string/hard_popcap_message
	config_entry_value = "The server is currently serving a high number of users, You cannot currently join. You may wait for the number of living crew to decline, observe, or find alternative servers."

/datum/config_entry/string/extreme_popcap_message
	config_entry_value = "The server is currently serving a high number of users, find alternative servers."

/datum/config_entry/flag/byond_member_bypass_popcap

/datum/config_entry/flag/panic_bunker	// prevents people the server hasn't seen before from connecting

/datum/config_entry/string/panic_bunker_message
	config_entry_value = "Sorry but the server is currently not accepting connections from never before seen players."

/datum/config_entry/number/notify_new_player_age	// how long do we notify admins of a new player
	min_val = -1

/datum/config_entry/number/notify_new_player_account_age	// how long do we notify admins of a new byond account
	min_val = 0

/datum/config_entry/flag/irc_first_connection_alert	// do we notify the irc channel when somebody is connecting for the first time?

/datum/config_entry/flag/check_randomizer

/datum/config_entry/string/ipintel_email

/datum/config_entry/string/ipintel_email/ValidateAndSet(str_val)
	return str_val != "ch@nge.me" && ..()

/datum/config_entry/number/ipintel_rating_bad
	config_entry_value = 1
	integer = FALSE
	min_val = 0
	max_val = 1

/datum/config_entry/number/ipintel_save_good
	config_entry_value = 12
	integer = FALSE
	min_val = 0

/datum/config_entry/number/ipintel_save_bad
	config_entry_value = 1
	integer = FALSE
	min_val = 0

/datum/config_entry/string/ipintel_domain
	config_entry_value = "check.getipintel.net"

/datum/config_entry/flag/aggressive_changelog

/datum/config_entry/flag/autoconvert_notes	//if all connecting player's notes should attempt to be converted to the database
	protection = CONFIG_ENTRY_LOCKED

/datum/config_entry/flag/allow_webclient

/datum/config_entry/flag/webclient_only_byond_members

/datum/config_entry/flag/announce_admin_logout

/datum/config_entry/flag/announce_admin_login

/datum/config_entry/flag/allow_map_voting
	deprecated_by = /datum/config_entry/flag/preference_map_voting

/datum/config_entry/flag/allow_map_voting/DeprecationUpdate(value)
	return value

/datum/config_entry/flag/preference_map_voting

/datum/config_entry/number/client_warn_version
	config_entry_value = null
	min_val = 500

/datum/config_entry/string/client_warn_message
	config_entry_value = "Your version of byond may have issues or be blocked from accessing this server in the future."

/datum/config_entry/flag/client_warn_popup

/datum/config_entry/number/client_error_version
	config_entry_value = null
	min_val = 500

/datum/config_entry/string/client_error_message
	config_entry_value = "Your version of byond is too old, may have issues, and is blocked from accessing this server."

/datum/config_entry/number/client_error_build
	config_entry_value = null
	min_val = 0

/datum/config_entry/number/minute_topic_limit
	config_entry_value = null
	min_val = 0

/datum/config_entry/number/second_topic_limit
	config_entry_value = null
	min_val = 0

/datum/config_entry/number/minute_click_limit
	config_entry_value = 400
	min_val = 0

/datum/config_entry/number/second_click_limit
	config_entry_value = 15
	min_val = 0

/datum/config_entry/number/error_cooldown	// The "cooldown" time for each occurrence of a unique error
	config_entry_value = 600
	integer = FALSE
	min_val = 0

/datum/config_entry/number/error_limit	// How many occurrences before the next will silence them
	config_entry_value = 50

/datum/config_entry/number/error_silence_time	// How long a unique error will be silenced for
	config_entry_value = 6000
	integer = FALSE

/datum/config_entry/number/error_msg_delay	// How long to wait between messaging admins about occurrences of a unique error
	config_entry_value = 50
	integer = FALSE

/datum/config_entry/flag/irc_announce_new_game
	deprecated_by = /datum/config_entry/string/chat_announce_new_game

/datum/config_entry/flag/irc_announce_new_game/DeprecationUpdate(value)
	return ""	//default broadcast

/datum/config_entry/string/chat_announce_new_game
	config_entry_value = null

/datum/config_entry/flag/debug_admin_hrefs

/datum/config_entry/number/mc_tick_rate/base_mc_tick_rate
	integer = FALSE
	config_entry_value = 1

/datum/config_entry/number/mc_tick_rate/high_pop_mc_tick_rate
	integer = FALSE
	config_entry_value = 1.1

/datum/config_entry/number/mc_tick_rate/high_pop_mc_mode_amount
	config_entry_value = 65

/datum/config_entry/number/mc_tick_rate/disable_high_pop_mc_mode_amount
	config_entry_value = 60

/datum/config_entry/number/mc_tick_rate
	abstract_type = /datum/config_entry/number/mc_tick_rate

/datum/config_entry/number/mc_tick_rate/ValidateAndSet(str_val)
	. = ..()
	if (.)
		Master.UpdateTickRate()

/datum/config_entry/flag/resume_after_initializations

/datum/config_entry/flag/resume_after_initializations/ValidateAndSet(str_val)
	. = ..()
	if(. && Master.current_runlevel)
		world.sleep_offline = !config_entry_value

/datum/config_entry/number/rounds_until_hard_restart
	config_entry_value = -1
	min_val = 0

/datum/config_entry/string/default_view
	config_entry_value = "15x15"

/datum/config_entry/string/default_view_square
	config_entry_value = "15x15"

/datum/config_entry/flag/log_pictures

/datum/config_entry/flag/picture_logging_camera


/datum/config_entry/flag/reopen_roundstart_suicide_roles

/datum/config_entry/flag/reopen_roundstart_suicide_roles_command_positions

/datum/config_entry/number/reopen_roundstart_suicide_roles_delay
	min_val = 30

/datum/config_entry/flag/reopen_roundstart_suicide_roles_command_report

/datum/config_entry/flag/auto_profile
