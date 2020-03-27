/* Runtime Condenser by Nodrak
 * Cleaned up and refactored by MrStonedOne
 * This will sum up identical runtimes into one, giving a total of how many times it occured. The first occurance
 * of the runtime will log the source, usr and src, the rest will just add to the total. Infinite loops will
 * also be caught and displayed (if any) above the list of runtimes.
 *
 * How to use:
 * 1) Copy and paste your list of runtimes from Dream Daemon into input.exe
 * 2) Run RuntimeCondenser.exe
 * 3) Open output.txt for a condensed report of the runtimes
 *
 * How to compile:
 * Requires visual c++ compiler 2012 or any linux compiler with c++11 support.
 * Windows:
 *	Normal: cl.exe /EHsc /Ox /Qpar Main.cpp
 *	Debug: cl.exe /EHsc /Zi Main.cpp
 * Linux:
 *	Normal: g++ -O3 -std=c++11 Main.cpp -o rc
 *	Debug: g++ -g -Og -std=c++11 Main.cpp -o rc
 * Any Compile errors most likely indicate lack of c++11 support. Google how to upgrade or nag coderbus for help..
 */

#include <iostream>
#include <fstream>
#include <cstring>
#include <cstdio>
#include <string>
#include <sstream>
#include <unordered_map>
#include <vector>
#include <algorithm>
#include <ctime>


#define PROGRESS_FPS 10
#define PROGRESS_BAR_INNER_WIDTH 50
#define LINEBUFFER (32*1024) //32KiB

using namespace std;

struct runtime {
	string text;
	string proc;
	string source;
	string usr;
	string src;
	string loc;
	unsigned int count;
};
struct harddel {
	string type;
	unsigned int count;
};
//What we use to read input
string * lastLine = new string();
string * currentLine = new string();
string * nextLine = new string();

//Stores lines we want to keep to print out
unordered_map<string,runtime> storedRuntime;
unordered_map<string,runtime> storedInfiniteLoop;
unordered_map<string,harddel> storedHardDel;

//Stat tracking stuff for output
unsigned int totalRuntimes = 0;
unsigned int totalInfiniteLoops = 0;
unsigned int totalHardDels = 0;


bool endofbuffer = false;
//like substr, but returns an empty string if the string is smaller then start, rather then an exception.
inline string safe_substr(string * S, size_t start = 0, size_t end = string::npos) {
	if (start > S->length())
		start = S->length();
	return S->substr(start, end);
}
//getline() is slow as fucking balls. this is quicker because we prefill a buffer rather then read 1 byte at a time searching for newlines, lowering on i/o calls and overhead. (110MB/s vs 40MB/s on a 1.8GB file pre-filled into the disk cache)
//if i wanted to make it even faster, I'd use a reading thread, a new line searching thread, another thread or four for searching for runtimes in the list to see if they are unique, and finally the main thread for displaying the progress bar. but fuck that noise.
inline string * readline(FILE * f) {
	static char buf[LINEBUFFER];
	static size_t pos = 0;
	static size_t size = 0;

	for (size_t i = pos; i < LINEBUFFER; i++) {
		char c = buf[i];
		if (i >= size && (pos || i < LINEBUFFER-1)) {
			if (feof(f) || ferror(f))
				break;
			if (size && pos) { //move current stuff to start of buffer
				size -= pos;
				i -= pos;
				memmove(buf, &buf[pos], size);
			}
			//fill remaining buffer
			size += fread(&buf[i], 1, LINEBUFFER-size-1, f);
			pos = 0;
			c = buf[i];
		}
		if (c == '\n') {
			//trim off any newlines from the start
			while (i > pos && (buf[pos] == '\r' || buf[pos] == '\n'))
				pos++;
			string * s = new string(&buf[pos], i-pos);
			pos = i+1;
			return s;
		}

	}
	string * s = new string(&buf[pos], size-pos);
	pos = 0;
	size = 0;
	endofbuffer = true;
	return s;
}

inline void forward_progress(FILE * inputFile) {
	delete(lastLine);
	lastLine = currentLine;
	currentLine	= nextLine;
	nextLine = readline(inputFile);
	//strip out any timestamps.
	if (nextLine->length() >= 10) {
		if ((*nextLine)[0] == '[' && (*nextLine)[3] == ':' && (*nextLine)[6] == ':' && (*nextLine)[9] == ']')
			nextLine->erase(0, 10);
		else if (nextLine->length() >= 26 && ((*nextLine)[0] == '[' && (*nextLine)[5] == '-' && (*nextLine)[14] == ':' && (*nextLine)[20] == '.' && (*nextLine)[24] == ']'))
			nextLine->erase(0, 26);
	}
}
//deallocates to, copys from to to.
inline void string_send(string * &from, string * &to) {
	delete(to);
	to = new string(*from);
}
inline void printprogressbar(unsigned short progress /*as percent*/) {
	double const modifer = 100.0L/(double)PROGRESS_BAR_INNER_WIDTH;
	size_t bars = (double)progress/modifer;
	cerr << "\r[" << string(bars, '=') << ((progress < 100) ? ">" : "") << string(PROGRESS_BAR_INNER_WIDTH-(bars+((progress < 100) ? 1 : 0)), ' ') << "] " << progress << "%";
	cerr.flush();
}

bool readFromFile(bool isstdin) {
	//Open file to read
	FILE * inputFile = stdin;
	if (!isstdin)
		inputFile = fopen("Input.txt", "r");

	if (ferror(inputFile))
		return false;
	long long fileLength = 0;
	clock_t nextupdate = 0;
	if (!isstdin) {
		fseek(inputFile, 0, SEEK_END);
		fileLength = ftell(inputFile);
		fseek(inputFile, 0, SEEK_SET);
		nextupdate = clock();
	}

	if (feof(inputFile))
		return false; //empty file
	do {
		//Update our lines
		forward_progress(inputFile);
		//progress bar

		if (!isstdin && clock() >= nextupdate) {
			int dProgress = (int)(((long double)ftell(inputFile) / (long double)fileLength) * 100.0L);
			printprogressbar(dProgress);
			nextupdate = clock() + (CLOCKS_PER_SEC/PROGRESS_FPS);
		}
		//Found a runtime!
		if (safe_substr(currentLine, 0, 14) == "runtime error:") {
			if (currentLine->length() <= 17) { //empty runtime, check next line.
				//runtime is on the line before this one. (byond bug)
				if (nextLine->length() < 2) {
					string_send(lastLine, nextLine);
				}
				forward_progress(inputFile);
				string * tmp = new string("runtime error: " + *currentLine);
				string_send(tmp, currentLine);
				delete(tmp);
			}
			//we assign this to the right container in a moment.
			unordered_map<string,runtime> * storage_container;

			//runtime is actually an infinite loop
			if (safe_substr(currentLine, 15, 23) == "Infinite loop suspected" || safe_substr(currentLine, 15, 31) == "Maximum recursion level reached") {
				//use our infinite loop container.
				storage_container = &storedInfiniteLoop;
				totalInfiniteLoops++;
				// skip the line about world.loop_checks
				forward_progress(inputFile);
				string_send(lastLine, currentLine);
			} else {
				//use the runtime container
				storage_container = &storedRuntime;
				totalRuntimes++;
			}

			string key = *currentLine;
			bool procfound = false; //so other things don't have to bother checking for this again.
			if (safe_substr(nextLine, 0, 10) == "proc name:") {
				key += *nextLine;
				procfound = true;
			}

			//(get the address of a runtime from (a pointer to a container of runtimes)) to then store in a pointer to a runtime.
			//(and who said pointers were hard.)
			runtime* R = &((*storage_container)[key]);

			//new
			if (R->text != *currentLine) {
				R->text = *currentLine;
				if (procfound) {
					R->proc = *nextLine;
					forward_progress(inputFile);
				}
				R->count = 1;

				//search for source file info
				if (safe_substr(nextLine, 2, 12) == "source file:") {
					R->source = *nextLine;
					//skip again
					forward_progress(inputFile);
				}
				//If we find this, we have new stuff to store
				if (safe_substr(nextLine, 2, 4) == "usr:") {
					forward_progress(inputFile);
					forward_progress(inputFile);
					//Store more info
					R->usr = *lastLine;
					R->src = *currentLine;
					if (safe_substr(nextLine, 2, 8) == "src.loc:") {
						R->loc = *nextLine;
						forward_progress(inputFile);
					}
				}

			} else { //existed already
				R->count++;
				if (procfound)
					forward_progress(inputFile);
			}

		} else if (safe_substr(currentLine, 0, 7) == "Path : ") {
			string deltype = safe_substr(currentLine, 7);
			if (deltype.substr(deltype.size()-1,1) == " ") //some times they have a single trailing space.
				deltype = deltype.substr(0, deltype.size()-1);

			unsigned int failures = strtoul(safe_substr(nextLine, 11).c_str(), NULL, 10);
			if (failures <= 0)
				continue;

			totalHardDels += failures;
			harddel* D = &storedHardDel[deltype];
			if (D->type != deltype) {
				D->type = deltype;
				D->count = failures;
			} else {
				D->count += failures;
			}
		}
	} while (!feof(inputFile) || !endofbuffer); //Until end of file
	if (!isstdin)
		printprogressbar(100);
	cerr << endl;
	return true;
}

bool runtimeComp(const runtime &a, const runtime &b) {
    return a.count > b.count;
}

bool hardDelComp(const harddel &a, const harddel &b) {
    return a.count > b.count;
}

bool writeToFile(bool usestdio) {
	//Open and clear the file
	ostream * output = &cout;
	ofstream * outputFile;
	if (!usestdio)
		output = outputFile = new ofstream("Output.txt", ios::trunc);


	if(usestdio || outputFile->is_open()) {
		*output << "Note: The source file, src and usr are all from the FIRST of the identical runtimes. Everything else is cropped.\n\n";
		if(storedInfiniteLoop.size() > 0)
			*output << "Total unique infinite loops: " << storedInfiniteLoop.size() << endl;

		if(totalInfiniteLoops > 0)
			*output << "Total infinite loops: " << totalInfiniteLoops << endl << endl;

		*output << "Total unique runtimes: " << storedRuntime.size() << endl;
		*output << "Total runtimes: " << totalRuntimes << endl << endl;
		if(storedHardDel.size() > 0)
			*output << "Total unique hard deletions: " << storedHardDel.size() << endl;

		if(totalHardDels > 0)
			*output << "Total hard deletions: " << totalHardDels << endl << endl;


		//If we have infinite loops, display them first.
		if(storedInfiniteLoop.size() > 0) {
			vector<runtime> infiniteLoops;
			infiniteLoops.reserve(storedInfiniteLoop.size());
			for (unordered_map<string,runtime>::iterator it=storedInfiniteLoop.begin(); it != storedInfiniteLoop.end(); it++)
				infiniteLoops.push_back(it->second);
			storedInfiniteLoop.clear();
			sort(infiniteLoops.begin(), infiniteLoops.end(), runtimeComp);
			*output << "** Infinite loops **";
			for (int i=0; i < infiniteLoops.size(); i++) {
				runtime* R = &infiniteLoops[i];
				*output << endl << endl << "The following infinite loop has occurred " << R->count << " time(s).\n";
				*output << R->text << endl;
				if(R->proc.length())
					*output << R->proc << endl;
				if(R->source.length())
					*output << R->source << endl;
				if(R->usr.length())
					*output << R->usr << endl;
				if(R->src.length())
					*output << R->src << endl;
				if(R->loc.length())
					*output << R->loc << endl;
			}
			*output << endl << endl; //For spacing
		}


		//Do runtimes next
		*output << "** Runtimes **";
		vector<runtime> runtimes;
		runtimes.reserve(storedRuntime.size());
		for (unordered_map<string,runtime>::iterator it=storedRuntime.begin(); it != storedRuntime.end(); it++)
			runtimes.push_back(it->second);
		storedRuntime.clear();
		sort(runtimes.begin(), runtimes.end(), runtimeComp);
		for (int i=0; i < runtimes.size(); i++) {
			runtime* R = &runtimes[i];
			*output << endl << endl << "The following runtime has occurred " << R->count << " time(s).\n";
			*output << R->text << endl;
			if(R->proc.length())
				*output << R->proc << endl;
			if(R->source.length())
				*output << R->source << endl;
			if(R->usr.length())
				*output << R->usr << endl;
			if(R->src.length())
				*output << R->src << endl;
			if(R->loc.length())
				*output << R->loc << endl;
		}
		*output << endl << endl; //For spacing

		//and finally, hard deletes
		if(totalHardDels > 0) {
			*output << endl << "** Hard deletions **";
			vector<harddel> hardDels;
			hardDels.reserve(storedHardDel.size());
			for (unordered_map<string,harddel>::iterator it=storedHardDel.begin(); it != storedHardDel.end(); it++)
				hardDels.push_back(it->second);
			storedHardDel.clear();
			sort(hardDels.begin(), hardDels.end(), hardDelComp);
			for(int i=0; i < hardDels.size(); i++) {
				harddel* D = &hardDels[i];
				*output << endl << D->type << " - " << D->count << " time(s).\n";
			}
		}
		if (!usestdio) {
			outputFile->close();
			delete outputFile;
		}
	} else {
		return false;
	}
	return true;
}

int main(int argc, const char * argv[]) {
	ios_base::sync_with_stdio(false);
	ios::sync_with_stdio(false);
	bool usestdio = false;
	if (argc >= 2 && !strcmp(argv[1], "-s"))
		usestdio = true;

	char exit; //Used to stop the program from immediately exiting
	cerr << "Reading input.\n";
	if(readFromFile(usestdio)) {
		cerr << "Input read successfully!\n";
	} else {
		cerr << "Input failed to open, shutting down.\n";
		if (!usestdio) {
			cerr << "\nEnter any letter to quit.\n";
			exit = cin.get();
		}
		return 1;
	}


	cerr << "Writing output.\n";
	if(writeToFile(usestdio)) {
		cerr << "Output was successful!\n";
		if (!usestdio) {
			cerr << "\nEnter any letter to quit.\n";
			exit = cin.get();
		}
		return 0;
	} else {
		cerr << "The output file could not be opened, shutting down.\n";
		if (!usestdio) {
			cerr << "\nEnter any letter to quit.\n";
			exit = cin.get();
		}
		return 1;
	}

	return 0;
}
