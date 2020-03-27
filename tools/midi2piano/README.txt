This is a remake of 2013 midi2piano tool.

Requirements:
  Python 3

Simply run midi2piano.py and choose midi file you want to convert.
The "sheet music" will be copied to the clipboard.

There are some constants defined at the top of midi2piano.py.

TICK_LAG - CHANGE THIS VALUE TO TICK LAG OF YOUR SERVER!

Change their value if needed.

LINE_LENGTH_LIM - max length of line allowed in the sheet music
LINES_LIMIT - max amount of lines allowed in the sheet music. Extra lines will be cropped.

OVERALL_IMPORT_LIM - max amount of characters allowed in the sheet music.


You can also transpose music if you need to
OCTAVE_TRANSPOSE - amount of octaves you melody will be shifted by
FLOAT_PRECISION - read comment

Additional notes:
1. Unlike previous midi2piano, this tool optimizes sheet music to fit more in less lines. 
2. If two notes are less than 50 ms apart, they are chorded. BYOND works in  1/10th of a second so 50 ms is time quanta.
4. MIDI event set_tempo is NOT supported. If your MIDI file uses set_tempo to change BPM significantly, consider using some other midi file.

This tool is considered final.

Made by EditorRUS/Delta Epsilon from Animus Station, ss13.ru
Contact me in Discord if you find any major issues: DeltaEpsilon#7787