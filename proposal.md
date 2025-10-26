# Sickness System

| Designers | Coders | Implemented | GitHub Links |
|---|---|---|---|
| JesterX | JesterX | No | TBD |

---

## Overview

The **Sickness System** introduces a variety of ailments that can affect the station’s crew.  
Sicknesses may be **viral**, **bacterial**, or **psychological** in nature.

Psychological sicknesses may also exist as **traits**.

Each sickness consists of:
- A sickness **name**
- A **cause type** (viral, bacterial, or psychological) – determines how it can be cured
- A **collection of stages**
- A **minimum viral or bacterial charge** required for contagion (spread through coughing or sneezing without masks or helmets; invisible in the atmos system and disperses like a gas)

### Sickness Stages

Stages represent phases of a sickness, including when they occur and what symptoms they cause.

Each stage defines:
- **Start Time:** How long before the stage begins  
- **Duration:** How long the stage lasts  
- **Symptom:** The effect applied to the character  

---

## Stage Types (Initial Release)

- **Cough**  
  - Player coughs, spreading germs to nearby crew (see *Contagion*).  
  - Appears as an emote.  
  - May be contagious.  
  - Contagion can be prevented with a surgical mask, helmet, etc.

- **Sneeze**  
  - Player sneezes, spreading germs to nearby crew (see *Contagion*).  
  - Appears as an emote.  
  - May be contagious.  
  - Prevented with masks or helmets.

- **Speak**  
  - Player says something randomly.  
  - Includes parameterized phrases (used for Tourette Syndrome and similar effects).

- **Perceive**  
  - Player perceives something unusual.  
  - Includes parameterized messages (used for paranoia or hallucinations).  
    - *Example:* “You think someone is watching you.”

- **Emote**  
  - Player performs an emote.  
  - Includes random emote messages.  
    - *Example:* “Player X shivers.”

- **Bleed**  
  - Applies bleeding over time.

- **Temperature Change**  
  - Adjusts body temperature (increase or decrease).  
  - Defines the amount of temperature change.

- **Fall Down**  
  - Player collapses to the ground.

- **Vomit**  
  - Player vomits.

- **Cured**  
  - Used for temporary sicknesses that resolve automatically.

- **Death** *(TBD)*  
  - Occurs if untreated.

> More symptoms will be added in future updates.

---

## Predefined Sicknesses

### Common Cold
**Cause:** Viral  

**Stages:**
- **Sneeze**  
  - Contagious: Yes  
  - Starts: Immediately  
  - Ends: Until cured  
  - Frequency: 1 minute  
- **Emote**  
  - Starts: 5 minutes  
  - Ends: Until cured  
  - Emote: *sniffles*  
- **Perceive**  
  - Starts: Immediately  
  - Ends: Until cured  
  - Perceptions:  
    - “Your throat feels sore.”  
    - “You feel tired.”  
  - Alleviating Compound: *Acetaminophen*  
- **Perceive**  
  - Starts: Immediately  
  - Ends: Until cured  
  - Perceptions:  
    - “Your nose is runny.”  
  - Alleviating Compound: None  
- **Cured**  
  - Starts: 30 minutes  
  - Ends: Once  
  - Grants Immunity: Yes  

---

### Flu
**Cause:** Viral  

**Stages:**
- **Cough**  
  - Contagious: Yes  
  - Starts: 5 minutes  
  - Ends: Until cured  
  - Frequency: 1 minute  
- **Emote**  
  - Starts: 5 minutes  
  - Ends: Until cured  
  - Emote: *sniffles*  
  - Frequency: 1 minute  
- **Perceive**  
  - Starts: 10 minutes  
  - Ends: Until cured  
  - Frequency: 1 minute  
  - Perceptions:  
    - “You feel extremely tired.”  
    - “Your body aches.”  
    - “You are sweating.”  
    - “Your head aches.”  
  - Alleviating Compound: *Acetaminophen*  
- **Perceive**  
  - Starts: 10 minutes  
  - Ends: Until cured  
  - Frequency: 1 minute  
  - Perceptions:  
    - “Your throat feels sore.”  
    - “Your nose is runny.”  
  - Alleviating Compound: None  
- **Temperature**  
  - Starts: 10 minutes  
  - Ends: Until cured  
  - Degree: *TBD*  
  - Alleviating Compound: *Acetaminophen*  
- **Cured**  
  - Starts: 45 minutes  
  - Ends: Once  
  - Grants Immunity: Yes  

---

### Tourette Syndrome
**Cause:** Psychological  

**Stages:**
- **Speak**  
  - Starts: Immediately  
  - Ends: Never  
  - Frequency: 1 minute  
  - Possible Phrases:  
    “Hey!”, “Okay!”, “No!”, “Yes!”, “Stop!”, “Uh!”, “Hmm!”, “What?”, “Go!”, “Ah!”, “Meow!”, “Woof!”, “Chirp!”, “Banana!”, “Car!”, “Blue!”

---

### Paranoia
**Cause:** Psychological  

**Stages:**
- **Perceive**  
  - Starts: Immediately  
  - Ends: Never  
  - Frequency: 1 minute  
  - Perceptions:  
    - “You think someone is observing you.”  
    - “You think someone wants you dead.”  
    - “You’re sure you just saw movement in your peripheral vision.”  
    - “You feel like your comms have a slight delay… someone might be listening.”

---

### Space Hallucinatory Syndrome
**Cause:** Psychological  

**Stages:**
- **Perceive**  
  - Starts: Immediately  
  - Ends: Never  
  - Frequency: 1 minute  
  - Perceptions:  
    - “You hear a faint laugh on the radio.”  
    - “Someone whispers, ‘I see you.’”  
    - “Footsteps echo.”  
    - “You hear typing sounds.”  
    - “The station alarm blips once.”  
    - “You hear children laughing.”

---

## Sickness Initialization

In the initial release:
- **Admins** can use a control panel to spawn predefined sicknesses.
- Admins choose the number of **Patient Zero** cases.

In future releases:
- Admins will be able to create, import, export, and modify **custom sicknesses**.

---

## Background

Sickness systems exist in several codebases.  
This is a **reimplementation** of the version previously developed for **UnityStation**.

---

## Planned Features

- A **Sickness Component** attached to all living beings, listing:  
  - Active sicknesses  
  - Time active  
  - Current stage  
- A **Sickness System** that periodically checks infected characters and triggers stage effects.  
- An **Admin UI Panel** for managing sickness events.  
- **New chemical compounds:**  
  - One for curing viral sicknesses  
  - One for curing bacterial sicknesses  
  - One for alleviating cold and flu symptoms  
- Updates to the **Medical Scanner** to detect sickness type.  
- **New traits** for psychological or allergic conditions.  
- A new **Psychology mechanic** to temporarily alleviate psychological illnesses.

---

## Game Design Rationale

- Adds chaos and realism to station life.  
- Introduces humorous or absurd sicknesses (e.g., paranoia, hallucinations, Tourette Syndrome).  
- Expands the role of **medical** and **psychology** staff.  
- Increases **chemist** utility for creating cures.  
- Encourages more **interdepartmental interaction**.  
- Enables future expansion (e.g., mysterious diseases, botany-based cures).

---

## Round Flow & Player Interaction

- Sicknesses can appear via:
  - Admin events (Patient Zero)
  - Character traits (e.g., Paranoia, Tourette)
- Player interactions include:
  - Medical analyzer  
  - Psychological intervention  
  - Taking medicine or pills  
  - Mixing chemical compounds  
  - Gameplay effects (chat, emotes, bleeding, temperature, vomiting)

**Affected Departments:**  
- All (can be infected)  
- Chemistry  
- Medicine  
- Psychology  
- *(Future)* Botany  

---

## Administrative & Server Impact

- Admins control sickness events.  
- Admins can create new sicknesses dynamically using import/export tools.  
- Minimal workload; unlikely to cause griefing or disputes.  
- No new rules required.

---

## Technical Considerations

- Adds a **Living Mob Component** that:  
  - Tracks active sicknesses  
  - Records time since infection and last symptom  
- A new system triggers symptoms when timers elapse.  
- Minimal performance impact.  
- Viral/bacterial **“special gases”** exist invisibly in atmos.  
- The initial **Admin Event UI** will include:  
  - Dropdown to select sickness type  
  - Field to choose the number of Patient Zero cases  
