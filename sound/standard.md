Standard audio file format is:
Ogg Vorbis on quality 5 preset, 44.1khz sample rate
Audio which plays with a source and direction (via playsound()) should be downmixed to mono, otherwise byond will do it itself.
Other sounds can be stereo

If you don't know what this means, here's a brief guide on how to do things right in audacity:

Project Rate (Hz) in the bottom right should be set to 44100 before exporting

If the sound is going to play to all players at the same time with no source, it can be left as stereo.

If it is going to play from a source with a direction, it should be mono.
You can downmix in audacity directly, use Tracks > Mix > Mix Stereo Down to Mono
If the option is greyed out, try clicking the track first. If it's still greyed out, it's likely already mono.

To export, go to File > Export > Export as OGG. Once the dialogue pops up, give the file a name, and set the Quality slider at the bottom to dead center so the bottom number displays 5.

That's it, you now have a properly exported file
