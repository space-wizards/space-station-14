# Sickness

| Designers | Coders | Implemented | GitHub Links |
|---|---|---|---|
| JesterX | JesterX | No | TBD |

## Overview

A basic sickness system (expendable in the future) that inflict a variety of ailments to the crew of the station.  They can be viral, bacterial and psychologic in nature.

Psychological sickness might be added as a trait.

All sickness are made of:
- The name of the sickness
- A kind of sickness cause (viral, bacterial or psychologic).  That will impact how the sickness can be cured later on.
- A collection of sickness stages.
- Minimal viral and bacterial charged needed to inflict contagion.  Created upon coughing and sneezing without mask and helmets.  (Invisible in the atmos system, disperses like any gas)

Sickness stages are different stages of each sickness, when they appear, what they do.

Description of what a sickness stage is:
- Time until that stage begins
- Duration of said stage
- What it does (called "Symptom")

Kind of stages that will be included in first release:

- Cough  
  - The player will cough, spreading germs near him, possibly infecting other crew member (see contagion below)
  - This will appear a an emote
  - Is the cough contagious or not
  - Contagion by cough is conterracted by surgical mask, helmet and such.
- Sneeze
  - The player will sneeze, spreading germs near him, possibly infecting other crew member (see contagion below)
  - This will appear as an emote
  - Is the sneeze contagious or not
  - Contagion by sneeze is conterracted by surgical mask, helmet and such.
- Speak
  - The player will say something
  - This includes a list of parametrized random things the player might say (for tourette syndrome and such)
- Percieve
  - The player will percieve something
  - This includes a list of parametrized random things the player might percieve (for paranoia and such)  Ex:  You think that someone is watching you.
- Emote
  - The player will emote something.
  - This includes a list of parametrized random things the player might emote  Ex:  Player X shivers.
- Bleed
  - The amount of bleeding
- Temperature change
  - The direction of temperature of the character (augment or decrease)
  - The quantity of temperature change
- Falls down on the ground
- Vomit
- Cured (to make some sicknesses temporary and auto-cured)
- Death (if not treated) (TBD)
  
More symptoms will be added in the future

List of pre-included sicknesses:

- Cold
    - Name : Common cold
    - Cause : Viral
    - Stages : 
        - Sneeze
            - Contagious : Yes
            - Start after: 0
            - End after : Until cured
            - Frequency : 1 minute.
        - Emote
            - Start after : 5 minutes
            - End after : Until cured
            - List of emotes : sniffles
        - Perceive
            - Start after : 0
            - End after : Until cured
            - List of perceptions : 
                - Your throat feels soar.
                - You feel tired.
            - Alleviating compound : acetaminophen                
        - Perceive
            - Start after : 0
            - End after : Until cured
            - List of perceptions : 
                - Your nose is runny.
            - Alleviating compound : None            
        - Cured
            - Start after : 30 minutes
            - End after : Once
            - Immunity : Yes

- Flu:
    - Name : Flu
    - Cause : Viral
    - Stages : 
        - Cough
            - Contagious : Yes
            - Start after: 5 minutes
            - End after : Until cured
            - Frequency : 1 minute.
        - Emote
            - Start after : 5 minutes
            - End after : Until cured
            - List of emotes : sniffles
            - Frequency : 1 minute.
        - Perceive
            - Start after : 10 minutes after sickness begins
            - End after : Until cured
            - Frequency : 1 minute.
            - List of perceptions : 
                - You feel seriously tired.
                - Your body aches.
                - You are sweating.
                - You head aches.
            - Alleviating compound : acetaminophen
        - Perceive
            - Start after : 10 minutes after sickness begins
            - End after : Until cured
            - Frequency : 1 minute.
            - List of perceptions : 
                - Your throat feels soar.
                - Your nose is runny.
            - Alleviating compound : None            
        - Temperature
            - Start after : 10 minutes
            - End after : Until cured
            - Degree : <TBD>
            - Alleviating compound : acetaminophen            
        - Cured
            - Start after : 45 minutes
            - End after : Once
            - Immunity : Yes            

- Tourette Syndrome:
    - Name : Tourette Syndrome
    - Cause : Psychological
    - Stages :
        - Say
            - Start after : 0
            - End after : Never
            - Frequency : 1 minute
            - List of things to say : Hey!” “Okay!” “No!” “Yes!” “Stop!” “Uh!” “Hmm!” “What?” “Go!” “Ah!”, “meow,” “woof,”, “chirp” ,“banana,” “car,” “blue!”

- Paranoia
    - Name : Paranoia
    - Cause : Psychological
    - Stages :
        - Percieve
        - Start after : 0
        - End after : Never
        - Frequency : 1 minute
        - List of thing to perceive :  "You think someone is observing you", "You think someone wants you dead", "You’re sure you just saw movement in your peripheral vision.", "You feel like your comms have a slight delay… someone might be listening."

- Hallucinations:
    - Name : Space hallucinatory syndrome
    - Cause : Psychological
    - Stages :
        - Percieve
        - Start after : 0
        - End after : Never
        - Frequency : 1 minute
        - List of thing to perceive : "You hear a faint laugh on the radio", Someone whispers, “I see you,”, "Footsteps echo" "The sound of typing…" "The station alarm blips once", "You hear childrens laughing"

How a sickness begins:
At first release, Admins will have panel will allow to spawn a specific pre-configured sicknesses, choosing the quantity of "patient zero".
In future release, Admins will have a panel to create a unique  custom sickness (import, export, modify) to their liking.

## Background

Sickness system in many forms exists in many code base.  This is a reimplementation of how I coded it for UnityStation.

## Features to be added

- A sickness component that all living being will have attached.  That will list what sicknesses the character has and how much time the sickness has been active (to determine the current stage).
- A sickness system that will check all characters with sicknesses to perform the current stages effect.
- An admin UI panel to start a sickness event
- New chemical compounds :  one for curing viral, one for curing bacterial, 1 for allievating cold and flu symptoms.
- Adjustments to the medical scanner to detect the kind of sickness and the type (viral, bacterial).
- New traits for some sicknesses (psychological, allergies and such).
- A new game mechanic for psychologists that alleviate psychological sicknesses for some time.

## Game Design Rationale

- The rounds will feel more alive and more chaotics.
- Some sicknesses are seriously silly (for instance : paranoia, hallucination and tourette syndrome.
- The psychologists and all medical staff will have fun alleviating symptoms and curing patients.
- The chemists will have new usefulness also to create the reagents to cure some sicknesses.
- The crew will interact more with the psychologist and medical staff.
- In the future, more mechanics might be added to find a cure for mysterious sicknesses.
- In the future, botany might also get involved.

## Roundflow & Player interaction

- Sickness will appear to a certain number of patient zero (Admin event) or by having the related trait (paranoia, tourette).
- Interraction will be with
    - Medical analyzer
    - Psychological intervention 
    - Taking specific pills
    - Making specific compounds
    - Several gameplay elements (chat, emote, bleed, temperature, vomit)
- Departments: 
    - All can be infected
    - Chemicals
    - Medicine
    - Psychologist
    - Eventually : Botany
    
## Administrative & Server Rule Impact (if applicable)

- Admins will be the one triggering the events
- Admins will eventually have fun creating new diseases on the fly.  That won't create much workload.  Import and Export features might be used.
- Not likely to create griefing, rule-breaking and player disputes.
- No new rules.

# Technical Considerations

- A living mob component will be added to list the sickness affecting a player, hom much time since the sicknesses are in their system how much time passed since last symptom.
- -A new system will be added to perform symptoms when timer is reached.
- Likely to not take a lot of processing power.
- There will be a viral / bacterial charge "special gases" invisible to atmos in the air.
- The UI for Admin event (first release) will only be a dropdown listing sicknesses and the amount of "patient zero" to infect.
