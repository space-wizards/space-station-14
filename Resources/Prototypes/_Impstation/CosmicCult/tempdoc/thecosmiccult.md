
# The Cosmic Cult

  

| Designers | Implemented | GitHub Links |

|---|---|---|

| AftrLite | :x: No | TBD |

  

## Overview

  

In SS13, cult gamemodes - both The Blood Cult and Clockwork Cult - are mechanically and thematically interesting; pitting factions against eachother wherein unlike classic Revolutionaries (Revs), cultists can be converted and de-converted back and forth, creating a tug of war between protagonistic and antagonistic forces aboard the station. 

However, the Clockwork Cult's reputation has been historically mixed for a variety of reasons. And while the Blood Cult has not suffered similarly, it concerns me from a visual design standpoint - security's primary colors are red and black, and Nuclear Operatives have a rather adjacent color palette. The Blood Cult, were it implemented, would only exacerbate this red-and-black homogeny. This emphasis on red palettes can potentially cause issues for players who have **Deuteranopia** or **Protanopia** (color blindness). 

In light of this, The Cosmic Cult is envisioned as a new SS14 "spin" on the SS13 Cult antagonist formulae, featuring all-new spritework in a distinguished color palette that tries to emphasise visual clarity.

  

### Breakdown

<sup>In this document, the default side players belong to will be referred to as either "crew" or "Command".</sup>
<sup>In this document, the antagonist to will be referred to as either "cult" or "The Cosmic Cult".</sup>
  

The Cosmic Cult is a team conversion antag: a small number of players begin as antagonists, but are able to convert crew to antags, with the antag faction's strength largely being dependent on the number of conversion. As a conversion antag, a round with The Cosmic Cult should attempt to follow the stages below.

  

 **Initial conversions**

- This is the stage where the antag begin converting crew - this should be done stealthily, and failing to do so may result in a swift win for crew. To their benefit the antag is given some kind of initial advantage, providing a quick power bump to get things going.

 **Expansion with risk**

- During this stage the antag faction grows, exhibiting signs and actions that may be more noticeable by crew. Staying stealthy becomes more difficult due to the number of antags growing, pushing the antags into more direct confrontration.

 **Confrontation with crew**

- The antag faction has been exposed and crew are now working to counteract the antags. Depending on how well the two previous stages have gone the crew may be in either a good or poor position to fight back.

 **Climax**

- An encounter between the antags and crew that largely decides the outcome of the round.

 **Resolution**
- After the climax the round finishes up, resolving the round for remaining players. Usually this is the last few minutes before the evacuation shuttle arrives.

  

Issues arise when one of these stages are too long, too short or entirely skipped over. A round doesn't necessarily have to be ruined just because a stage is misplaced, however if there are no sufficient back-up scenarios to fall back on the round may end up frustrating for some amount of the round's players (e.g. stalling, dragged out fighting, antag gets found too early without getting to affect the round, no resolution so players just evacuate).

  

This design document will try to ensure that a round with a Cult antag follows this pattern.

  

### Cosmic Cult Gamemode Goals

  

The goals that we want to reach with the Cosmic Cult are the following:

- The Cosmic Cult should be applicable as its own round type and as a mid-round antag, equivalent to Nukies/LoneOp or Traitors/Mid-round Traitors. The should be big differences in the initial number of antags and their available resources.

- Round removal should be discouraged where possible. It should not be the optimal choice to gib or leave a player to rot unless in specific circumstances.

- The Cosmic Cult should not be discovered through *no fault of their own* in the Initial Conversions phase (i.e. metagameable mechanics).

- A single non-Security crew member discovering an antag should not spell the end of the antag faction's progression in the round.

- Round end conditions should be clear and straight-forward, though a crew overcoming the antag shouldn't necessarily mean the end of the round.

- Crew shouldn't be able to hard counterplay the antag without engaging with the antag.

  
  

## The Cosmic Cult, Basic layout

<sup>Any values given here are suggestions and are especially marked by being **bolded**. Any such value should be adjusted based on playtesting.</sup>

  

The Cosmic Cult antag begins as a roundstart antag roll for the Cosmic Cult gamemode. No midround rolls are currently being considered.

Cosmic Cult antags should be prompted to elect a Leader (CultLead) from the initial pool of cultists. This allows for a degree of in-character mentorship, as the most knowledgeable/experienced player can be democratically allowed to take charge of the antag faction for that round, creating an environment that may be more conducive to new player experiences. 
This is necesary for the cult to succeed, as the Cult Leader is tasked with placing The Monument, the focal point of The Cosmic Cult's gameplay loop. The primary goals for the Cult Leader is to act as an organizing force for the cult, as well as converting crew to grow the faction's strength.

If the Cult Leader dies or is Deconverted, the Cult - after a short waiting period - should be prompted to elect a new Leader.

Cosmic Cultists can be identified by other Cosmic Cultists via an icon next to their character, with the Cult Leader having an emboldened version of the regular Cosmic Cult antag icon. 

For crew, the goal is to deconvert cult converts, protect heads/command, and ensure the cultists are discovered.

  

### Win Conditions

  

Win conditions for a round with a Cosmic Cult antag are the following:

  

- **Cosmic Cult Complete Victory:** The Cult has converted its Command Target, and The Cult uses the completed *Monument* to summon *The Unknown*, annihilating the station.

- **Cosmic Cult Major Victory:** The Cult uses the completed *Monument* to summon *The Unknown*, and the station is annihilated.

- **Cosmic Cult Minor Victory:** The Cult completes *The Monument*, and there are no *alive, unrestrained* Command *on the evac shuttle.*

- **Cult-Crew Tie:** The Cult *does not* complete *The Monument*, and there are no *alive, unrestrained* Command *on the evac shuttle.*

- **Crew Minor Victory:** There are is >1 *alive, unrestrained* Command *on the evac shuttle* when Evac reaches Centcomm.

- **Crew Major Victory:** There are is >1 *alive, unrestrained* Command *on the evac shuttle* when Evac reaches Centcomm, and there is no *unrestrained* Cult Lead *on the evac shuttle* when Evac reaches CentComm.

- **Crew Complete Victory:** *All* cultists are *deconverted* or *dead* when Evac reaches CentComm.

  

The round-end screen could look like the following:

  

```

Cosmic Cult Complete Victory! [Green text]

The Unknown has been summoned forth. [Purple text]

TARGETPLAYERNAME%, COMMANDJOB% was converted to the Cosmic Cult. [Green text]

X Entropy was Siphoned. [Yellow text]

X% of Crew were converted to the Cosmic Cult. [Yellow text]

```

  

```

Cosmic Cult Major Victory! [Green text]

The Unknown has been summoned forth. [Purple text]

X Entropy was Siphoned. [Yellow text]

X% of Crew were converted to the Cosmic Cult. [Yellow text]

TARGETPLAYERNAME%, COMMANDJOB% was not converted to the Cosmic Cult. [Red text]
```

  

```

Cosmic Cult Minor Victory! [Green text]

The Monument was completed! [Green text]

0 Unconverted Command made it to CentComm. [Green text]

X Entropy was Siphoned. [Yellow text]

X% of Crew were converted to the Cosmic Cult. [Yellow text]

TARGETPLAYERNAME%, COMMANDJOB% was not converted to the Cosmic Cult. [Red text]
```


```

What a mess! [Yellow text]

The Monument was not completed! [Red text]

No members of Commmand made it to CentComm. [Red text]

```


```

Crew Minor Victory! [Green text]

>0 Members of Commmand made it to CentComm. [Red text]

```


```

Crew Major Victory! [Green text]

>0 Members of Commmand made it to CentComm. [Red text]

CULTLEADERNAME% was in Custody. [Red text]

```


```

Crew Complete Victory! [Green text]

All Cosmic Cultists were killed or deconverted! [Red text]

```

  

## The Monument, Entropy, Conversion, and the Masquerade.

  

The Monument is how the Cosmic Cult antag faction grows its power, and is a *requirement* to obtain Cosmic Cult Victory. The monument is a large 3x3 Structure that is initially only visibile to members of The Cosmic Cult. It is placed by the Cult Leader, is entirely indestructible, and only one Monument can exist at at time.

The Monument should contain an Upgrade Tree, accessible by the Cult Leader. This upgrade tree allows the Cult Leader to spend Entropy on upgrades, giving the cult access to more items and abilities, as well as furthering The Monument's *Stage*.

The Monument's stages are influenced by Conversion and its Upgrade Tree. Once a segment of the Upgrade Tree has been fully completed, the next stage of The Monument can be unlocked by spending Entropy. This allows the cult to slowly advance even if they are having difficulty converting crewmembers. 

In addition, The Monument should automatically advance stages as the cult converts crew - progressing to *Stage 2* once 25% of the crew are Cosmic Cultists, and *Stage 3* once 45% of the crew are Cosmic Cultists. This rewards the cult for converting crew.



### Entropy

  
"Entropy" is the primary resource the Cosmic Cult uses to progress. Entropy can be obtained by using the **[Siphon Entropy]** ability on any humanoid target. Entropy can - and should be - be deposited into *The Monument*.

Entropy could probably stand to have more uses.
  

### Conversion

Conversion is the most overarching way for the Cult to increase their power. Crew can be converted by positioning them on an *Invocation Circle* and activating it with a Cultist's *Compass of Stars*. Cultists can scribe *Invocation Cirlces* near *The Monument.* Initially, only non-mindshielded crew can be converted. Once *The Monument* has reached Stage 3, all crew can be converted, no matter what.

### Deconversion

Deconversion is the most immediate way for crew to fight back against the Cosmic Cult. It should be relatively easy when targeting an isolated Cultist, and very easy when performed by a Chaplain.

Deconversion begins when at least 40u of Holy Water is metabolized by a Cultist. After a waiting period, the Cultist loses their antag status, and is successfully deconverted. Deconversion puts you fully on the crew side, and strips your memories of having been in the cult. Because of this, security may find it pertinent to interrogate a cultist -before- prompting their deconversion.

A deconverted Cultist can *always* be reconverted again.
  
  
### Mindshields

  

Mindshield implants are a way to add extra safety for vital station personnel. While they provide a counter to the cult's initial conversion, it is not a true hardcounter, as The Cosmic Cult can obtain means to negate mindshields entirely by upgrading *The Monument*.

Command and Security are the only roles that have mindshields roundstart.

Having a mindshield does not change Cultist status or antag faction; a Cultist can be mindshielded and still belong to the Cosmic Cult. Mindshields only protects the owner from Conversion.

New mindshields should be able to be purchased from Cargo at a reasonable price. One possible suggestion would be 4 implanters for $4000.

### The Masquerade

As the Cosmic Cult advances throughout the round, their ability to conceal themselves - and their activities - will gradually wear away. This extends in a variety of ways, from messages broadcast in the chat box to cultists being visually marked as cultists to anyone who sees them.

The broadcast messages could look like the following:
```
At 20% crew conversion,
??? ANNOUNCEMENT:
The atmosphere aboard the station grows cold. Something is slowly gaining influence.
```

```
At 30% crew conversion,
??? ANNOUNCEMENT:
An unnerving numbness prickles your senses. A cult is growing in power!
```

```
At 40% crew conversion,
??? ANNOUNCEMENT:
Arcs of bluespace energy crackle across the station's groaning structure. 
The end soon approaches.
```
At 45% crew conversion, or once *The Monument* reaches Stage 3: after a short delay, all Cultists are marked with a glowing, star-shaped visual effect that clearly discloses that they are a member of the Cosmic Cult.

## Cosmic Cult Items

  

To assist in their goals of ushering in the end of all things, Cosmic Cultists have access to a multitude of items. The choice and utilization of these items should provide players options and flavors for how their experience of the gamemode plays out. It's important that the items do not encourage gameplay that go against the Cosmic Cult Gamemode Goals.


- Compass of Stars: Part of a Cultist's starting gear in the form of an ability that allows them to summon and unsommon a compass to an open hand slot, therefore not taking up inventory space. Should be upgradeable through The Monument's upgrade tree.

- Entropic Armor: A powerful but heavy hardsuit that slows the wearer. While equipped, it should provide access to an ability that allows the user to quickly dash a short distance, with a 25 second cooldown.

- Entropic Blade: A slow but powerful melee weapon that inflicts both Airloss, Cold, and Slash.

- Vacuous Lance: A *very* slow yet strong melee weapon that inflicts both Airloss, Cold, and Puncture. Can be thrown. When impacting someone after being thrown, it should inflict Zero Gravity and a very potent knockback impulse, sending the unfortunate target flying. Should be able to be recalled by the user who threw it, similar to the Ninja's Katana.

- Nullimov(*): A Cosmic Cult-aligned AI lawboard. Must still be installed in the AI law upload.
*(*I'm sure someone has a punnier name than this. Please suggest one.)*

- Bluespace Lodestar: Spawns a Cosmic Cult-aligned shuttle out in space, with **3** Cosmic Cultist ghost roles aboard that are equipped with combat gear.

  

## Expansions

- More items could be implemented for more Cosmic Cult playstyles.

- Certain events could trigger based on conversion thresholds; e.g. ERT or Deathsquad if station reaches 80% conversion.

- This design document is a WIP and more iteration is always welcome to improve gameplay design.
