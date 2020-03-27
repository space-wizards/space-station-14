/obj/mecha/combat/honker
	desc = "Produced by \"Tyranny of Honk, INC\", this exosuit is designed as heavy clown-support. Used to spread the fun and joy of life. HONK!"
	name = "\improper H.O.N.K"
	icon_state = "honker"
	step_in = 3
	max_integrity = 140
	deflect_chance = 60
	internal_damage_threshold = 60
	armor = list("melee" = -20, "bullet" = 0, "laser" = 0, "energy" = 0, "bomb" = 0, "bio" = 0, "rad" = 0, "fire" = 100, "acid" = 100)
	max_temperature = 25000
	infra_luminosity = 5
	operation_req_access = list(ACCESS_THEATRE)
	internals_req_access = list(ACCESS_MECH_SCIENCE, ACCESS_THEATRE)
	wreckage = /obj/structure/mecha_wreckage/honker
	add_req_access = 0
	max_equip = 3
	var/squeak = TRUE

/obj/mecha/combat/honker/get_stats_part()
	var/integrity = obj_integrity/max_integrity*100
	var/cell_charge = get_charge()
	var/datum/gas_mixture/int_tank_air = internal_tank.return_air()
	var/tank_pressure = internal_tank ? round(int_tank_air.return_pressure(),0.01) : "None"
	var/tank_temperature = internal_tank ? int_tank_air.temperature : "Unknown"
	var/cabin_pressure = round(return_pressure(),0.01)
	var/output = {"[report_internal_damage()]
						[integrity<30?"<font color='red'><b>DAMAGE LEVEL CRITICAL</b></font><br>":null]
						[internal_damage&MECHA_INT_TEMP_CONTROL?"<font color='red'><b>CLOWN SUPPORT SYSTEM MALFUNCTION</b></font><br>":null]
						[internal_damage&MECHA_INT_TANK_BREACH?"<font color='red'><b>GAS TANK HONK</b></font><br>":null]
						[internal_damage&MECHA_INT_CONTROL_LOST?"<font color='red'><b>HONK-A-DOODLE</b></font> - <a href='?src=[REF(src)];repair_int_control_lost=1'>Recalibrate</a><br>":null]
						<b>IntegriHONK: </b> [integrity]%<br>
						<b>PowerHONK charge: </b>[isnull(cell_charge)?"No powercell installed":"[cell.percent()]%"]<br>
						<b>Air source: </b>[use_internal_tank?"Internal Airtank":"Environment"]<br>
						<b>AirHONK pressure: </b>[tank_pressure]kPa<br>
						<b>AirHONK temperature: </b>[tank_temperature]&deg;K|[tank_temperature - T0C]&deg;C<br>
						<b>HONK pressure: </b>[cabin_pressure>WARNING_HIGH_PRESSURE ? "<font color='red'>[cabin_pressure]</font>": cabin_pressure]kPa<br>
						<b>HONK temperature: </b> [return_temperature()]&deg;K|[return_temperature() - T0C]&deg;C<br>
						<b>Lights: </b>[lights?"on":"off"]<br>
						[dna_lock?"<b>DNA-locked:</b><br> <span style='font-size:10px;letter-spacing:-1px;'>[dna_lock]</span> \[<a href='?src=[REF(src)];reset_dna=1'>Reset</a>\]<br>":null]
					"}
	return output

/obj/mecha/combat/honker/get_stats_html()
	var/output = {"<html>
						<head><title>[src.name] data</title>
						<style>
						body {color: #00ff00; background: #32CD32; font-family:"Courier",monospace; font-size: 12px;}
						hr {border: 1px solid #0f0; color: #fff; background-color: #000;}
						a {padding:2px 5px;;color:#0f0;}
						.wr {margin-bottom: 5px;}
						.header {cursor:pointer;}
						.open, .closed {background: #32CD32; color:#000; padding:1px 2px;}
						.links a {margin-bottom: 2px;padding-top:3px;}
						.visible {display: block;}
						.hidden {display: none;}
						</style>
						<script language='javascript' type='text/javascript'>
						[js_byjax]
						[js_dropdowns]
						function SSticker() {
						    setInterval(function(){
						        window.location='byond://?src=[REF(src)]&update_content=1';
						        document.body.style.color = get_rand_color_string();
						      document.body.style.background = get_rand_color_string();
						    }, 1000);
						}

						function get_rand_color_string() {
						    var color = new Array;
						    for(var i=0;i<3;i++){
						        color.push(Math.floor(Math.random()*255));
						    }
						    return "rgb("+color.toString()+")";
						}

						window.onload = function() {
							dropdowns();
							SSticker();
						}
						</script>
						</head>
						<body>
						<div id='content'>
						[src.get_stats_part()]
						</div>
						<div id='eq_list'>
						[src.get_equipment_list()]
						</div>
						<hr>
						<div id='commands'>
						[src.get_commands()]
						</div>
						</body>
						</html>
					 "}
	return output

/obj/mecha/combat/honker/get_commands()
	var/output = {"<div class='wr'>
						<div class='header'>Sounds of HONK:</div>
						<div class='links'>
						<a href='?src=[REF(src)];play_sound=sadtrombone'>Sad Trombone</a>
						<a href='?src=[REF(src)];play_sound=bikehorn'>Bike Horn</a>
						<a href='?src=[REF(src)];play_sound=airhorn2'>Air Horn</a>
						<a href='?src=[REF(src)];play_sound=carhorn'>Car Horn</a>
						<a href='?src=[REF(src)];play_sound=party_horn'>Party Horn</a>
						<a href='?src=[REF(src)];play_sound=reee'>Reee</a>
						<a href='?src=[REF(src)];play_sound=weeoo1'>Siren</a>
						<a href='?src=[REF(src)];play_sound=hiss1'>Hissing Creature</a>
						<a href='?src=[REF(src)];play_sound=armbomb'>Armed Grenade</a>
						<a href='?src=[REF(src)];play_sound=saberon'>Energy Sword</a>
						<a href='?src=[REF(src)];play_sound=airlock_alien_prying'>Airlock Prying</a>
						<a href='?src=[REF(src)];play_sound=lightningbolt'>Lightning Bolt</a>
						<a href='?src=[REF(src)];play_sound=explosionfar'>Distant Explosion</a>
						</div>
						</div>
						"}
	output += ..()
	return output


/obj/mecha/combat/honker/get_equipment_list()
	if(!equipment.len)
		return
	var/output = "<b>Honk-ON-Systems:</b><div style=\"margin-left: 15px;\">"
	for(var/obj/item/mecha_parts/mecha_equipment/MT in equipment)
		output += "<div id='[REF(MT)]'>[MT.get_equip_info()]</div>"
	output += "</div>"
	return output

/obj/mecha/combat/honker/play_stepsound()
	if(squeak)
		playsound(src, "clownstep", 70, 1)
	squeak = !squeak

/obj/mecha/combat/honker/Topic(href, href_list)
	..()
	if (href_list["play_sound"])
		switch(href_list["play_sound"])
			if("sadtrombone")
				playsound(src, 'sound/misc/sadtrombone.ogg', 50)
			if("bikehorn")
				playsound(src, 'sound/items/bikehorn.ogg', 50)
			if("airhorn2")
				playsound(src, 'sound/items/airhorn2.ogg', 40) //soundfile has higher than average volume
			if("carhorn")
				playsound(src, 'sound/items/carhorn.ogg', 80) //soundfile has lower than average volume
			if("party_horn")
				playsound(src, 'sound/items/party_horn.ogg', 50)
			if("reee")
				playsound(src, 'sound/effects/reee.ogg', 50)
			if("weeoo1")
				playsound(src, 'sound/items/weeoo1.ogg', 50)
			if("hiss1")
				playsound(src, 'sound/voice/hiss1.ogg', 50)
			if("armbomb")
				playsound(src, 'sound/weapons/armbomb.ogg', 50)
			if("saberon")
				playsound(src, 'sound/weapons/saberon.ogg', 50)
			if("airlock_alien_prying")
				playsound(src, 'sound/machines/airlock_alien_prying.ogg', 50)
			if("lightningbolt")
				playsound(src, 'sound/magic/lightningbolt.ogg', 50)
			if("explosionfar")
				playsound(src, 'sound/effects/explosionfar.ogg', 50)
	return
