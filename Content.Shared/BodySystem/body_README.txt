


	The limb system for ss14 is fairly simple but extremely modular. There are four main classes you should know about:
	
	- BodyPart: This class stores the data for a single body part, such as an arm or a leg. It is also a prototype, so you can define 
	  your own pre-defined body parts (e.g. human_basic) in a YAML file. Note that this class has an ID that can be named anything, but 
	  please try and stick to the pattern: "bodyPart.torso.basic_human". bodyPart.[BodyPartType].[Description].
	  
	- BodyTemplate: This class is a prototype as well. Every entity body requires a "frame" to be built around - the bodytemplate defines
	  the slots that BodyParts can go in, defines their connections, and their names. For instance, the "humanoid" BodyTemplate defines the
	  "right arm" as a BodyPartType.arm slot and states that any arm in this slot is connected to the "torso" and "right hand", which are also
	  defined.
	  
	- BodyPreset: This class allows for quick loading of a template by defining what kinds of bodyparts go in a template. For instance, the "basic_human"
	  preset uses the BodyTemplate "humanoid" and has the ID's of all the basic_human BodyPart prototypes assigned to all the slots in the humanoid BodyTemplate.
	  
	- BodyManagerComponent: Attached to an entity, this component manages all of these body classes. It has a specific BodyTemplate that the attached entity
	  follows and a Dictionary<string, BodyPart> which maps slotNames to their corresponding bodyPart, if there is one. 
	  
	These allow for a robust and modular system. Normally, humanoid BodyTemplates only have a basic frame - two arms, two legs, etc, I'm sure you know what your 
	body looks like. But it's quite easy to edit BodyTemplates in-game and pass it to the BodyManagerComponent, which has functions to merge it into the current 
	template. 
	BodyPart fields:
		Durability, DestroyThreshold, Resistance: The HP of this limb. Durability is the max and base HP. Once HP hits zero, a limb is broken. If it gets to the DestroyThreshold, it will fall off or be destroyed
												  entirely. Resistance is a WIP armor system for mechanisms.
		BodyPartType specific fields: Some BodyPartTypes have unique fields, such as integer field "length" for arms, used to determine reach length. These are stored in Dictionary<string,string> data.
		BodyPartCompatibility: Mechanical and biological limbs cannot be connected. However, a bionic link mechanism can be installed to make a limb able to universally connect (see next section)
	
	
	Now, we will introduce mechanisms. Mechanism are analogue to ss13's robot components (remember: binary communicators, cameras, etc.). These are no longer unique to robots,
	as even fully biological creatures have hearts in their chest. Every BodyPart has a size (e.g. humans have a size 8 torso) that determines how many mechanisms can be fit into
	the torso. Some mechanisms can only be installed into biological limbs or only mechanical limbs. Mechanisms may require power too, which can be drawn from a battery mechanism 
	from any limb, to any limb (doesn't take into account limb type). Some hypothetical examples:
	- Size 0 Xeno Skin Modification: Installable through a special submersion tube and gives you xeno powers. Biological limbs only.
	- Size 7 Internal Fusion Reactor: Too big to fit normally into a human, but you can always remove your liver to make room. Creates massive amounts of energy. Mechanical limbs only.
	- Size 4 Camera Uplink: Activate to open up a station camera in a popup window, but only one can be open at a time. Costs a small amount of energy every second during use. 
	- Size 1 Lithium Gel Battery: Stores lots of energy for its size, but slowly drains itself if you aren't moving.
	- Size 3 HONK-module: HONK!
	Those are non-existent examples, but there are many basic mechanisms that represent basic creature functionality or other functions previously built into ss13:
	- Size 1 Heart: If you don't have one you're going to die.
	- Size 1 Camera: Allows you to see. Consumes energy every second.
	- Size 1 Eyes: Allows you to see. Metabolism increases to accomodate.
	- Size 1 Brain: Your consciousness.
	- Size 1 Posibrain: A mechanical brain.
	- Size 1 Law Obedience Module: Forces the user to obey a lawset. Can be modified through an upload console or manually. Requires a posibrain in the same BodyPart.
	- Size 1 AI Obedience Module: Allows for forced obedience to an AI. Requires a posibrain in the same BodyPart.
	  
	  
	  
	  
	  
	  
	  