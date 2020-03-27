"""
This module allows user to convert MIDI melodies to SS13 sheet music ready
for copy-and-paste
"""
from functools import reduce
import midi as mi
import easygui as egui
import pyperclip as pclip

LINE_LENGTH_LIM = 50
LINES_LIMIT = 200
TICK_LAG = 0.5
OVERALL_IMPORT_LIM = 2*LINE_LENGTH_LIM*LINES_LIMIT
END_OF_LINE_CHAR = """
""" # BYOND can't parse \n and I am forced to define my own NEWLINE char

OCTAVE_TRANSPOSE = 0 # Change here to transpose melodies by octaves
FLOAT_PRECISION = 2 # Change here to allow more or less numbers after dot in floats

OCTAVE_KEYS = 12
HIGHEST_OCTAVE = 8

time_quanta = 100 * TICK_LAG
"""
class Meta():
    version = 1.0
    integer = 1
    anti_integer = -1
    maximum = 1000
    epsilon = 0.51
    delta_epsilon = -0.1
    integral = []
    tensor = [[],[],[]]
    o_complexity = epsilon**2
    random_variance = 0.01
"""

# UTILITY FUNCTIONS
def condition(event):
    """
    This function check if given MIDI event is meaningful
    """
    if event[0] == 'track_name' and event[2] == 'Drums': # Percussion
        return False
    if event[0] == 'note': # Only thing that matters
        return True
    return False

def notenum2string(num, accidentals, octaves):
    """
    This function converts given notenum to SS13 note according to previous
    runs expressed using _accidentals_ and _octaves_
    """
    names = ['C', 'C#', 'D', 'D#', 'E', 'F', 'F#', 'G', 'G#', 'A', 'A#', 'B']
    convert_table = {1:0, 3:1, 6:2, 8:3, 10:4}
    inclusion_table = {0:0, 2:1, 5:2, 7:3, 9:4}

    num += OCTAVE_KEYS * OCTAVE_TRANSPOSE
    octave = int(num / OCTAVE_KEYS)
    if octave < 1 or octave > HIGHEST_OCTAVE:
        return ["", accidentals, octaves]

    accidentals = accidentals.copy()
    octaves = octaves.copy()

    output_octaves = list(octaves)
    name_indx = num % OCTAVE_KEYS

    accidental = (len(names[name_indx]) == 2)
    output_octaves[name_indx] = octave
    add_n = False

    if accidental:
        accidentals[convert_table[name_indx]] = True
    else:
        if name_indx in inclusion_table:
            add_n = accidentals[inclusion_table[name_indx]]
            accidentals[inclusion_table[name_indx]] = False

    return [
        (
            names[name_indx]+
            ("n" if add_n else "")+
            str((octave if octave != octaves[name_indx] else ""))
        ),
        accidentals,
        output_octaves
        ]

def dur2mod(dur, bpm_mod=1.0):
    """
    This functions returns float representation of duration ready to be
    added to the note after /
    """
    mod = bpm_mod / dur
    mod = round(mod, FLOAT_PRECISION)
    return str(mod).rstrip('0').rstrip('.')
# END OF UTILITY FUNCTIONS

# CONVERSION FUNCTIONS
def obtain_midi_file():
    """
    Asks user to select MIDI and returns this file opened in binary mode for reading
    """
    file = egui.fileopenbox(msg='Choose MIDI file to convert',
                            title='MIDI file selection',
                            filetypes=[['*.mid', 'MID files']])
    if not file:
        return None
    file = open(file, mode='rb').read()
    return file

def midi2score_without_ticks(midi_file):
    """
    Transforms aforementioned file into a score, truncates it and returns it
    """
    opus = mi.midi2opus(midi_file)
    opus = mi.to_millisecs(opus)
    score = mi.opus2score(opus)
    return score[1:] # Ticks don't matter anymore, it is always 1000

def filter_events_from_score(score):
    """
    Filters out irrevelant events and returns new score
    """
    return list(map( # For each score track
        lambda score_track: list(filter( # Filter irrevelant events
            condition,
            score_track
            )),
        score
        ))

def filter_empty_tracks(score):
    """
    Filters out empty tracks and returns new score
    """
    return list(filter(
        lambda score_track: score_track,
        score))


def filter_start_time_and_note_num(score):
    """
    Recreates score with only note numbers and start time of each note and returns new score
    """
    return list(map(
        lambda score_track: list(map(
            lambda event: [event[1], event[4]],
            score_track)),
        score))

def merge_events(score):
    """Merges all tracks together and returns new score"""
    return list(reduce(
        lambda lst1, lst2: lst1+lst2,
        score))

def sort_score_by_event_times(score):
    """Sorts events by start time and returns new score"""
    return list(map(
        lambda index: score[index],
        sorted(
            list(range(len(score))),
            key=lambda indx: score[indx][0])
        ))

def convert_into_delta_times(score):
    """
    Transform start_time into delta_time and returns new score
    """
    return list(map(
        lambda super_event: (
            [
                super_event[1][0]-super_event[0][0],
                super_event[0][1]
            ]), # [ [1, 2], [3, 4] ] -> [ [2, 2] ]
        zip(score[:-1], score[1:]) # Shifted association. [1, 2, 3] -> [ (1, 2), (2, 3) ]
        ))+[[1000, score[-1][1]]] # Add 1 second note to the end

def perform_roundation(score):
    """
    Rounds delta times to the nearest multiple of time quanta as BYOND can't
    process duration less than that and returns new score
    """
    return list(map(
        lambda event: [time_quanta*round(event[0]/time_quanta), event[1]],
        score))

def obtain_common_duration(score):
    """
    Returns the most frequent duration throughout the whole melody
    """
    # Parse durations and filter out 0s
    durs = list(filter(lambda x: x, list(map(lambda event: event[0], score))))
    unique_durs = []
    for dur in durs:
        if dur not in unique_durs:
            unique_durs.append(dur)
    # How many such durations occur throughout the melody?
    counter = [durs.count(dur) for dur in unique_durs]
    highest_counter = max(counter) # Highest counter
    dur_n_count = list(zip(durs, counter))
    dur_n_count = list(filter(lambda e: e[1] == highest_counter, dur_n_count))
    return dur_n_count[0][0] # Will be there

def reduce_score_to_chords(score):
    """
    Reforms score into a chord-duration list:
    [[chord_notes], duration_of_chord]
    and returns it
    """
    new_score = []
    new_chord = [[], 0]
    # [ [chord notes], duration of chord ]
    for event in score:
        new_chord[0].append(event[1]) # Append new note to the chord
        if event[0] == 0:
            continue # Add new notes to the chord until non-zero duration is hit
        new_chord[1] = event[0] # This is the duration of chord
        new_score.append(new_chord) # Append chord to the list
        new_chord = [[], 0] # Reset the chord
    return new_score

def obtain_sheet_music(score, most_frequent_dur):
    """
    Returns unformated sheet music from score
    """
    result = ""

    octaves = [3 for i in range(12)]
    accidentals = [False for i in range(7)]
    for event in score:
        for note_indx in range(len(event[0])):
            data = notenum2string(event[0][note_indx], accidentals, octaves)
            result += data[0]
            accidentals = data[1]
            octaves = data[2]
            if note_indx != len(event[0])-1:
                result += '-'

        if event[1] != most_frequent_dur: # Quarters are default
            result += '/'
            result += dur2mod(event[1], most_frequent_dur)
        result += ','

    return result

def explode_sheet_music(sheet_music):
    """
    Splits unformatted sheet music into formated lines of LINE_LEN_LIM
    and such and returns a list of such lines
    """
    split_music = sheet_music.split(',')
    split_music = list(map(lambda note: note+',', split_music))
    split_list = []
    counter = 0
    line_counter = 1
    for note in split_music:
        if line_counter > LINES_LIMIT-1:
            break
        if counter+len(note) > LINE_LENGTH_LIM-2:
            split_list[-1] = split_list[-1].rstrip(',')
            split_list[-1] += END_OF_LINE_CHAR
            counter = 0
            line_counter += 1
        split_list.append(note)
        counter += len(note)

    return split_list

def finalize_sheet_music(split_music, most_frequent_dur):
    """
    Recreates sheet music from exploded sheet music, truncates it and returns it
    """
    sheet_music = ""
    for note in split_music:
        sheet_music += note
    sheet_music = sheet_music.rstrip(',') # Trim the last ,
    sheet_music = "BPM: " + str(int(60000 / most_frequent_dur)) + END_OF_LINE_CHAR + sheet_music
    return sheet_music[:min(len(sheet_music), OVERALL_IMPORT_LIM)]
# END OF CONVERSION FUNCTIONS

def main_cycle():
    """
    Activate the script
    """
    while True:
        midi_file = obtain_midi_file()
        if not midi_file:
            return # Cancel
        score = midi2score_without_ticks(midi_file)
        score = filter_events_from_score(score)
        score = filter_start_time_and_note_num(score)
        score = filter_empty_tracks(score)
        score = merge_events(score)
        score = sort_score_by_event_times(score)
        score = convert_into_delta_times(score)
        score = perform_roundation(score)
        most_frequent_dur = obtain_common_duration(score)
        score = reduce_score_to_chords(score)
        sheet_music = obtain_sheet_music(score, most_frequent_dur)
        split_music = explode_sheet_music(sheet_music)
        sheet_music = finalize_sheet_music(split_music, most_frequent_dur)

        pclip.copy(sheet_music)

main_cycle()
