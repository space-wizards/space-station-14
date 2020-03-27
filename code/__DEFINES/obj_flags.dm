// Flags for the obj_flags var on /obj


#define EMAGGED					(1<<0)
#define IN_USE					(1<<1) // If we have a user using us, this will be set on. We will check if the user has stopped using us, and thus stop updating and LAGGING EVERYTHING!
#define CAN_BE_HIT				(1<<2) //can this be bludgeoned by items?
#define BEING_SHOCKED			(1<<3) // Whether this thing is currently (already) being shocked by a tesla
#define DANGEROUS_POSSESSION	(1<<4) //Admin possession yes/no
#define ON_BLUEPRINTS			(1<<5)  //Are we visible on the station blueprints at roundstart?
#define UNIQUE_RENAME			(1<<6) // can you customize the description/name of the thing?
#define USES_TGUI				(1<<7)	//put on things that use tgui on ui_interact instead of custom/old UI.
#define FROZEN					(1<<8)
#define BLOCK_Z_FALL			(1<<9) // Should this object block z falling?

// If you add new ones, be sure to add them to /obj/Initialize as well for complete mapping support

// Flags for the item_flags var on /obj/item

#define BEING_REMOVED			(1<<0)
#define IN_INVENTORY			(1<<1) //is this item equipped into an inventory slot or hand of a mob? used for tooltips
#define FORCE_STRING_OVERRIDE	(1<<2) // used for tooltips
#define NEEDS_PERMIT			(1<<3) //Used by security bots to determine if this item is safe for public use.
#define SLOWS_WHILE_IN_HAND		(1<<4)
#define NO_MAT_REDEMPTION			(1<<5) // Stops you from putting things like an RCD or other items into an ORM or protolathe for materials.
#define DROPDEL						(1<<6) // When dropped, it calls qdel on itself
#define NOBLUDGEON				(1<<7)		// when an item has this it produces no "X has been hit by Y with Z" message in the default attackby()
#define ABSTRACT				(1<<9) 	// for all things that are technically items but used for various different stuff
#define IMMUTABLE_SLOW			(1<<10) // When players should not be able to change the slowdown of the item (Speed potions, etc)
#define IN_STORAGE				(1<<11) //is this item in the storage item, such as backpack? used for tooltips
#define SURGICAL_TOOL			(1<<12)	//Tool commonly used for surgery: won't attack targets in an active surgical operation on help intent (in case of mistakes)

// Flags for the clothing_flags var on /obj/item/clothing

#define LAVAPROTECT (1<<0)
#define STOPSPRESSUREDAMAGE		(1<<1)	//SUIT and HEAD items which stop pressure damage. To stop you taking all pressure damage you must have both a suit and head item with this flag.
#define BLOCK_GAS_SMOKE_EFFECT	(1<<2)	// blocks the effect that chemical clouds would have on a mob --glasses, mask and helmets ONLY!
#define MASKINTERNALS				    (1<<3)		// mask allows internals
#define NOSLIP                  (1<<4)   //prevents from slipping on wet floors, in space etc
#define THICKMATERIAL				(1<<5)	//prevents syringes, parapens and hypos if the external suit or helmet (if targeting head) has this flag. Example: space suits, biosuit, bombsuits, thick suits that cover your body.
#define VOICEBOX_TOGGLABLE (1<<6) // The voicebox in this clothing can be toggled.
#define VOICEBOX_DISABLED (1<<7) // The voicebox is currently turned off.
#define SCAN_REAGENTS (1<<9) // Allows helmets, masks and glasses to scan reagents.
#define BLOCKS_SHOVE_KNOCKDOWN (1<<10) // Prevents shovies against a dense object from knocking the wearer down.
#define SNUG_FIT               (1<<11) //Prevents knock-off from things like hat-throwing.
#define ANTI_TINFOIL_MANEUVER   (1<<12) //Hats with negative effects when worn (i.e the tinfoil hat).

/// Flags for the organ_flags var on /obj/item/organ

#define ORGAN_SYNTHETIC			(1<<0)	//Synthetic organs, or cybernetic organs. Reacts to EMPs and don't deteriorate or heal
#define ORGAN_FROZEN			(1<<1)	//Frozen organs, don't deteriorate
#define ORGAN_FAILING			(1<<2)	//Failing organs perform damaging effects until replaced or fixed
#define ORGAN_EXTERNAL			(1<<3)	//Was this organ implanted/inserted/etc, if true will not be removed during species change.
#define ORGAN_VITAL				(1<<4)	//Currently only the brain
#define ORGAN_SYNTHETIC_EMP		(1<<5)	//Synthetic organ affected by an EMP. Deteriorates over time.
