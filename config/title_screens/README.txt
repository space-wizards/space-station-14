The enclosed /images folder holds the image files used as the title screen for the game. All common formats such as PNG, JPG, and GIF are supported.
Byond's DMI format is also supported, but if you use a DMI only include one image per file and do not give it an icon_state (the text label below the image).

Keep in mind that the area a title screen fills is a 480px square so you should scale/crop source images to these dimensions first.
The game won't scale these images for you, so smaller images will not fill the screen and larger ones will be cut off.

Using unnecessarily huge images can cause client side lag and should be avoided. Extremely large GIFs should preferentially be converted to DMIs.
Placing non-image files in the images folder can cause errors.

You may add as many title screens as you like, if there is more than one a random screen is chosen (see name conventions for specifics).

---

Naming Conventions:

Every title screen you add must have a unique name. It is allowed to name two things the same if they have different file types, but this should be discouraged.
Avoid using the plus sign "+" and the period "." in names, as these are used internally to classify images.


Common Titles:

Common titles are in the rotation to be displayed all the time. Any name that does not include the character "+" is considered a common title.

An example of a common title name is "clown".

The common title screen named "default" is special. It is only used if no other titles are available. Because default only runs in the 
absence of other titles, if you want it to also appear in the general rotation you must name it something else.


Map Titles:

Map titles are tied to a specific in game map. To make a map title you format the name like this "(name of a map)+(name of your title)"

The spelling of the map name is important. It must match exactly the define MAP_NAME found in the relevant .DM file in the /_maps folder in 
the root directory. It can also be seen in game in the status menu. Note that there are no spaces between the two names.

It is absolutely fine to have more than one title tied to the same map.

An example of a map title name is "Omegastation+splash".


Rare Titles:

Rare titles are a just for fun feature where they will only have a 1% chance of appear in in the title screen pool of a given round.
Add the phrase "rare+" to the beginning of the name. Again note there are no spaces. A title cannot be rare title and a map title at the same time.

An example of a rare title name is "rare+explosion"