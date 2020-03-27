The enclosed /sounds folder holds the sound files used as the title music for the game. OGG and WAV are supported.

Using unnecessarily huge sounds can cause client side lag and should be avoided.

You may add as many title sounds as you like, if there is more than one a random screen is chosen (see name conventions for specifics).

---

Naming Conventions:

Every title sound you add must have a unique name. It is allowed to name two things the same if they have different file types, but this should be discouraged.
Avoid using the plus sign "+" and the period "." in names, as these are used internally to classify sounds.


Common Title Sounds:

Common sounds are in the rotation to be displayed all the time. Any name that does not include the character "+" is considered a common sound.

An example of a common sound name is "clown".


Map Title Sounds:

Map sounds are tied to a specific in game map. To make a map title you format the name like this "(name of a map)+(name of your sound)"

The spelling of the map name is important. It must match exactly the define MAP_NAME found in the relevant .DM file in the /_maps folder in 
the root directory. It can also be seen in game in the status menu. Note that there are no spaces between the two names.

It is absolutely fine to have more than one sound tied to the same map. It's also fine to have a rare map sound.

An example of a map sound name is "Omegastation+splash".


Rare Title Sounds:

Rare title sounds are a just for fun feature where they will only have a 1% chance of appear in in the title sound pool of a given round.
Add the phrase "rare+" to the beginning of the name. Again note there are no spaces.

An example of a rare sound name is "rare+explosion"
