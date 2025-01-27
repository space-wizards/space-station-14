Okay, so like the main dev site says, things in game are handled with a data model they call the Entity Component System. For adding items to the game, or changing how they behave, unless you want some radically new behavior (like making puddles of fuel explode when they get too hot or touch fire, for example) you don't have to worry about the underlying engine too much at all. We can just take what exists and combine it in new ways. It DOES help to have some coding knowledge if things go wrong, but it's not 100% required.

So let's say I want to add a new kind of grenade that spawns, I dunno, killer tomatoes when it explodes. I go into the codebase and look in the Resources folder:

![image](https://github.com/Gh0ulcaller/imp-station-14/assets/97265903/aa3478fd-478c-48c8-a6bb-8e21f6e6ccfc)

This folder has basically everything the server uses to spawn any of the stuff you see in game.  All of the game sprites are inTextures, all of the Sound cues are in audio, and all of the stuff that defines an item or a person or literally anything else, is in the Prototypes folder. I'm using an IDE, or integrated development environment, just software that makes a lot of coding stuff easier. This is the open source version of VSCode called VSCodium the SS14 dev wiki reccomends on the Setting Up a Development Environment page ([found here](https://docs.spacestation14.com/en/general-development/setup/setting-up-a-development-environment.html)), but you can do this in notepad or any text editor, it'll just be less robust.

Alright back to the grenade example. Grenades already exist in game, we can use one of them as a base to build off of. So I go into the prototype folder and search for grenade, and it brings up a few results:

![image](https://github.com/Gh0ulcaller/imp-station-14/assets/97265903/119f46d0-7756-4825-afd1-c1a2c249ed86)

Let's just go with a basic hand grenade, I think it's safe to assume that would be the second file located at Prototypes\Entities\Objects\Weapons\Throwable\grenades.yml. Opening it up in VSCodium, there's a few definitions inside. This will look intimidating if you're not used to code, but we can break it down into parts pretty easily. The very first section of the file is this:

![image](https://github.com/Gh0ulcaller/imp-station-14/assets/97265903/ff531b5a-2dde-414c-b200-62a2ff9dab83)

This is the core of every grenade in the game. It defines how grenades work, how they interact with things, and what can be done with or to them. Let's break it down further.


This topmost section defines things about the entity rather than functions. This is the start to any prototype definition you make. You define what kind of prototype it is (entity), if it has a parent that it inherits properties from (parent: BaseItem), and what it should be called in the game when it needs to be found/what name other prototypes should call it if they need to reference it (id:GrenadeBase). The "abstract:true" line just means that it's not a real item, and that it can't be spawned anywhere. By default it's assumed to be false, so you only need to include it when you're making basic building blocks like this.

![image](https://github.com/Gh0ulcaller/imp-station-14/assets/97265903/d2476f94-790b-4457-b25d-2c35e52aedc2)

This next section is where the meat of things is. This is the components section, where everything about how an entity will function and behave is defined.

![image](https://github.com/Gh0ulcaller/imp-station-14/assets/97265903/e049afc6-c1d3-405a-8c87-3eff449f113b)

This is a Sprite component, it tells the game where the spritesheet for the object is located. The layers section tells the game which parts of the sprite sheet to load (each part has a state with a name, in this case icon) and what order to load them in, one on top of the other. The grenade sprite is slightly more complex than a sprite with a single state, because it has different states that get assigned to it, but we don't really need to worry about it for making a grenade that spawns killer tomatoes because it's fine if it looks the same as every other grenade. We don't need to touch this.

![image](https://github.com/Gh0ulcaller/imp-station-14/assets/97265903/e6ee0b35-2f9f-42a9-9abc-2f16c71d5d6a)

These next few components are pretty basic and self-explanatory, but I want to review them anyway just to hammer a point home. These basically say that a hand grenade is an item, that it's a small item, that it counts as a piece of clothing you can equip, that it cannot be equipped quickly, and that it goes in the belt slot. Each line that starts with -type: is the start of a new component and therefore a new behavior definition. The structure is really important. The first letter of each subsection under the initial -type: line NEEDS to line up with the t in type. If the file structure is ever wrong somewhere, it breaks the whole file. In my experience, none of the items in that file will exist in the game if it's broken.

![image](https://github.com/Gh0ulcaller/imp-station-14/assets/97265903/26d4351f-c7e4-42ff-b255-1ca4b87cf16d)

These components contain some pretty important stuff for how a grenade works. The first one tells the game that when a player activates this item to start a 3.5 second countdown until triggering an event. This is what eventually allows grenades to actually explode in the system. The next component tells the game that the item can be damaged, and what types of damage it can take. The damageContainer setting refers to what group of damageable items this belongs to, in this case Inorganic, which sets what types of damage it can actually take. Again, not something we need to worry about for what we're doing here, but worth explaining to understand how this all fits together. If you want to look into it more yourself, the file to look for is "Prototypes\Damage\groups.yml". The third section is more complicated, but basically tells the game that the item can be destroyed with enough damage, and what that damage threshold is. Once more, not important for our use case, we'll just use the default for every hand grenade.

![image](https://github.com/Gh0ulcaller/imp-station-14/assets/97265903/c1b0a2d7-03ba-4f9f-849c-149b5e72cf8e)

The last few components just sets up some visual stuff, we really don't need to worry about this for our case. For the sake of completion, this is here so that every grenade that uses this base gets added to the game's Appearance and AnimationPlayer systems and will have things for those systems applied to them.The very last component handles swapping appearances/sprites between a grenade's default state and the primed state when it gets used, where the little blinking light in the bottom right corner of the sprite starts blinking. Again, do not worry about it. We're leaving this as is.

![image](https://github.com/Gh0ulcaller/imp-station-14/assets/97265903/819aca6a-fff0-4f8f-9f49-9cbba84ae7e0)




Okay, so that's the stuff that makes up the core of every hand grenade in the game. How does this work with specific grendate types? Let's look at the very next entity definition in the file.

![image](https://github.com/Gh0ulcaller/imp-station-14/assets/97265903/78c25915-1918-487b-a39c-63dc7869e747)

This is the entity definition for your basic explosive hand grenade. It has the GrenadeBase entity we looked at above as a parent, which means it has all of the same components that the GrenadeBase has. A hand grenade spawned with this entity Prototype will get all of the basic grenade components, and then all of the components from this defnintion on top of that. That's how all of these inheritance systems work. If an entity has a parent, it gets all of the components that parent has, and then adds its own components to them. Now, let's look at the components specific to the explosive hand grenade.

![image](https://github.com/Gh0ulcaller/imp-station-14/assets/97265903/93435d48-4b65-4320-8cc2-d653b4520610)

There's a few things happening here. The second component tells the game that this is an explosive, and that when it explodes that explosion has the defined attributes. The first component just tells the game that when the entity is triggered, it will explode. The third component tells the game that when the entity is triggered, it should play the sound once when the trigger happens, and again 2 seconds later. How does that trigger happen?

![image](https://github.com/Gh0ulcaller/imp-station-14/assets/97265903/76d396c5-4ed4-44a4-8a83-e227818f7648)

This is from the first definition, the grenade base. It's the same type of component that the child grenade, with the id ExGrenade, uses to set up the beep sound. What's happening here is a really important concept for working in this system and getting things to do what you want them to. When a child entity has the same kind of component defined as its parent, it adds on top of and potentially overwrites the settings of the parent component. If you're at all familiar with object oriented programming, this is the same concept as a base class and a class extension. Let's represent this visually:

![image](https://github.com/Gh0ulcaller/imp-station-14/assets/97265903/b3631e2e-48dd-4418-9692-c6fe408cad9c)

This is what the WHOLE OnUseTimerTrigger component would look like for the ExGrenade definition if everything it gets from the parent was shown in the definition. It's literally one extra line, but it's super important to understand how this type of inheritence works if you're going to be doing things in this system. Now, let's say the ExGrenade definition did have this line, and that the number was 4 instead. This would overwrite what was in the parent definition, and the grenade would trigger and explode in 4 seconds instead of 3.5. Whenever a child definition has the same component as its parent, with the same setting variable defined (in this case delay: 4 instead of delay: 3.5), the value given in the child definition gets used instead of the one given in the parent definition. Again, an extremely important concept to grasp for all of this. I hope I've made it as clear as possible.




Okay, let's leave behind the world of inheritence and parent/child interactions for now. We have our OnUseTimerTrigger component, that tells the game to cause a trigger 3 1/2 seconds after the item gets used. We have our ExplodeOnTrigger component, which tells the game that when this item triggers, an explosion should happen. How do we make this spawn killer tomatoes? We have to add another kind of OnTrigger component to our definition. But what kind of OnTrigger components are there?

![image](https://github.com/Gh0ulcaller/imp-station-14/assets/97265903/326f1a8a-9d85-496c-80ee-e68ad4e175c1)

This is where using an IDE is super handy. What I'm doing here is searching the contents of every .yml file in the Resources folder for OnTrigger components. Every one of these OnTrigger components can be added to any item in the game, as long as it also has some way to activate a trigger. This isn't showing every OnTrigger component that exists, but it's most of them. Let's check out the SpawnOnTrigger component from one of the definitions in the floor_trap.yml file.

![image](https://github.com/Gh0ulcaller/imp-station-14/assets/97265903/d745eda3-b963-46dd-b1bd-a5334cc8f7f8)

This is exactly what we need. This particular floor trap is set up so that whenever another entity collides with it, it spawns a Space Carp, and then deletes the object. That's very similar to what we're trying to do. Let's set up a new definition for our grenade then, using BaseGrenade as a parent. I've just added this to the end of the grenades.yml file for now, but it's worth thinking about where your definition might best belong when doing something like this. As long as you have the right id, you can reference any Prototype definition from any other file in the Protypes folder. Just make sure you're not giving two things the same id.

![image](https://github.com/Gh0ulcaller/imp-station-14/assets/97265903/4e0e3136-7699-4992-8d9f-b9347918627c)

So now we've got our brand new killer tomato grenade, using the GrenadeBase as the parent and copying the OnUseTimerTrigger component from the ExGrenade, SpawnOnTrigger from the floor trap to spawn whatever entity Prototype we put in, and finally DeleteOnTrigger which will just delete the grenade entity after it explodes since there's no explosion to damage it and destroy it otherwise. But, that's the wrong mob, we don't want carp, we're a plant-based explosive. So, we delve into the Prototype folder again looking for the killer tomato definition.

![image](https://github.com/Gh0ulcaller/imp-station-14/assets/97265903/ee4f49c6-3278-450d-aefb-7fa3d9cf9d57)

There it is, in the file Prototypes\Entities\Mobs\NPCs\miscellaneous.yml. I really do reccomend using an IDE for this stuff, you can just manually look through every file in the directory and search for what you're looking for, but it takes a lot longer. Let's add it to our grenade instead of the space carp.

![image](https://github.com/Gh0ulcaller/imp-station-14/assets/97265903/c8393b18-78f6-4bdf-ac00-130ef5f07b7a)

That's all! Now we've got a grenade that spawns a killer tomato when it goes off. But, that's kind of boring by itself, and it can't explode because then the tomato just dies when it does. What if we used a smoke grenade as a base instead? We go through the same process as before. We scroll down in grenades.yml and grab the id for the smoke grenade Prototype definition, and use that as the parent in place of GrenadeBase, then check the components to see if there's anything we want to overwrite in our tomato grenade definition.

![image](https://github.com/Gh0ulcaller/imp-station-14/assets/97265903/ff7a7a69-bf74-4f44-9825-9ee6428956d2)

That all looks fine, but let's tone down the duration and spread of the smoke effect. This should just provide a little cover for our little monster and cause some chaos and uncertainty, not douse a huge area of the station in smoke for  a bit.

![image](https://github.com/Gh0ulcaller/imp-station-14/assets/97265903/41879077-b5e1-4f28-a956-14c5ebd4733c)

And with that, we're done! We now have a grenade that lets out a cloud of smoke and spawns a killer tomato when it goes off. But, as of now there's no way to get this item without an admin spawning it in. Let's fix that. I think the best spot for something like this would be randomly finding it in maintenence containers. Just to spice things up. Let's look for something that spawns in those same lockers then: Strange Pills.

![image](https://github.com/Gh0ulcaller/imp-station-14/assets/97265903/2b3542b9-93f3-40c3-a3fd-4f2f2aa319f5)

Okay, there we go. So we go to the file where this is declared, Prototypes\Catalog\Fills\Lockers\misc.yml, and find these definitions. There's two of them, and their ids are ClosetMaintenenceFilledRandom and ClosetMaintenenceFilledRandom. These handle the spawning lists and probablilities for the items you find in maintenence containers.

![image](https://github.com/Gh0ulcaller/imp-station-14/assets/97265903/093312a1-0d96-4b02-b44f-efdd406c9087)

Simple enough. So we just set the id to TomatoGrenade, and set the probability to whatever makes sense, then add it to both definitions. In this case, something that spawns a hostile mob when used should probably be pretty rare. I'll set it to 0.01, or 1%.

![image](https://github.com/Gh0ulcaller/imp-station-14/assets/97265903/67849646-5355-4f35-95d6-5596202425cf)

And...that's it! We've walked through the whole process of how to take things that already exist in the Prototype definitions and recombine them into something new, and then making that new thing available in game. Hopefully this guide was as clear and understandable as I tried to make it.
