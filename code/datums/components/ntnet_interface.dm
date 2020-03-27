//Thing meant for allowing datums and objects to access a NTnet network datum.
/datum/proc/ntnet_receive(datum/netdata/data)
	return

/datum/proc/ntnet_receive_broadcast(datum/netdata/data)
	return

/datum/proc/ntnet_send(datum/netdata/data, netid)
	var/datum/component/ntnet_interface/NIC = GetComponent(/datum/component/ntnet_interface)
	if(!NIC)
		return FALSE
	return NIC.__network_send(data, netid)

/datum/component/ntnet_interface
	var/hardware_id			//text. this is the true ID. do not change this. stuff like ID forgery can be done manually.
	var/network_name = ""			//text
	var/list/networks_connected_by_id = list()		//id = datum/ntnet
	var/differentiate_broadcast = TRUE				//If false, broadcasts go to ntnet_receive. NOT RECOMMENDED.

/datum/component/ntnet_interface/Initialize(force_name = "NTNet Device", autoconnect_station_network = TRUE)			//Don't force ID unless you know what you're doing!
	hardware_id = "[SSnetworks.get_next_HID()]"
	network_name = force_name
	if(!SSnetworks.register_interface(src))
		. = COMPONENT_INCOMPATIBLE
		CRASH("Unable to register NTNet interface. Interface deleted.")
	if(autoconnect_station_network)
		register_connection(SSnetworks.station_network)

/datum/component/ntnet_interface/Destroy()
	unregister_all_connections()
	SSnetworks.unregister_interface(src)
	return ..()

/datum/component/ntnet_interface/proc/__network_receive(datum/netdata/data)			//Do not directly proccall!
	SEND_SIGNAL(parent, COMSIG_COMPONENT_NTNET_RECEIVE, data)
	if(differentiate_broadcast && data.broadcast)
		parent.ntnet_receive_broadcast(data)
	else
		parent.ntnet_receive(data)

/datum/component/ntnet_interface/proc/__network_send(datum/netdata/data, netid)			//Do not directly proccall!

	if(netid)
		if(networks_connected_by_id[netid])
			var/datum/ntnet/net = networks_connected_by_id[netid]
			return net.process_data_transmit(src, data)
		return FALSE
	for(var/i in networks_connected_by_id)
		var/datum/ntnet/net = networks_connected_by_id[i]
		net.process_data_transmit(src, data)
	return TRUE

/datum/component/ntnet_interface/proc/register_connection(datum/ntnet/net)
	if(net.interface_connect(src))
		networks_connected_by_id[net.network_id] = net
	return TRUE

/datum/component/ntnet_interface/proc/unregister_all_connections()
	for(var/i in networks_connected_by_id)
		unregister_connection(networks_connected_by_id[i])
	return TRUE

/datum/component/ntnet_interface/proc/unregister_connection(datum/ntnet/net)
	net.interface_disconnect(src)
	networks_connected_by_id -= net.network_id
	return TRUE
