# In-code keypress handling system

This whole system is heavily based off of forum_account's keyboard library.
Thanks to forum_account for saving the day, the library can be found
[here](https://secure.byond.com/developer/Forum_account/Keyboard)!

.dmf macros have some very serious shortcomings. For example, they do not allow reusing parts
of one macro in another, so giving cyborgs their own shortcuts to swap active module couldn't
inherit the movement that all mobs should have anyways. The webclient only supports one macro,
so having more than one was problematic. Additionally each keybind has to call an actual
verb, which meant a lot of hidden verbs that just call one other proc. Also our existing
macro was really bad and tied unrelated behavior into `Northeast()`, `Southeast()`, `Northwest()`,
and `Southwest()`.

The basic premise of this system is to not screw with .dmf macro setup at all and handle
pressing those keys in the code instead. We have every key call `client.keyDown()`
or `client.keyUp()` with the pressed key as an argument. Certain keys get processed
directly by the client because they should be doable at any time, then we call
`keyDown()` or `keyUp()` on the client's holder and the client's mob's focus.
By default `mob.focus` is the mob itself, but you can set it to any datum to give control of a
client's keypresses to another object. This would be a good way to handle a menu or driving
a mech. You can also set it to null to disregard input from a certain user.

Movement is handled by having each client call `client.keyLoop()` every game tick.
As above, this calls holder and `focus.keyLoop()`. `atom/movable/keyLoop()` handles movement
Try to keep the calculations in this proc light. It runs every tick for every client after all!

You can also tell which keys are being held down now. Each client a list of keys pressed called
`keys_held`. Each entry is a key as a text string associated with the world.time when it was
pressed.

No client-set keybindings at this time, but it shouldn't be too hard if someone wants.

Notes about certain keys:

* `Tab` has client-sided behavior but acts normally
* `T`, `O`, and `M` move focus to the input when pressed. This fires the keyUp macro right away.
* `\` needs to be escaped in the dmf so any usage is `\\`

You cannot `TICK_CHECK` or check `world.tick_usage` inside of procs called by key down and up
events. They happen outside of a byond tick and have no meaning there. Key looping
works correctly since it's part of a subsystem, not direct input.