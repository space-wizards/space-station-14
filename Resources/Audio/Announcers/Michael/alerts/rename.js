// Set the method variable to one of these
const methods = [
	'startsWith', // Replaces beginning with replace if matches searchfor  OR  cuts off the beginning if matches searchfor if replace is undefined
	'endsWith', // Replaces end with replace if matches searchfor  OR  cuts off the end if matches searchfor if replace is undefined
	'includes', // Replaces all instances of searchfor with replace  OR  removes all instances of searchfor if replace is undefined

	'numberedSuffix', // Adds a numbered suffix to the end of the file name
	'numberedPrefix', // Adds a numbered prefix to the beginning of the file name
	'numbered', // Replaces the file name with a number
];

// User set variables
const dirname = './'; // Where to look for files ('./' is the current directory)
const includedirs = false; // Whether or not to include directories in the search
const searchfor = 'code_'; // What to search for
const replace = undefined; // What to replace with (undefined will remove the searchfor from the file name entirely)
const removeextraperiods = true; // Whether or not to remove extra periods in the file name (good for numbered methods)
const method = 'startsWith'; // Which method to use (see above)
const startingnumber = 0; // What number to start at (only used with numbered methods)
const debuglog = true; // Provides some extra info about what's going on
// End of user set variables


// Check if method is valid
if (typeof method !== 'string' || !methods.includes(method)) return console.log(`ERROR:  Invalid method ${method}`);
else console.log('INFO:  Using method: ' + method);

// Check if dirname is valid
if (typeof dirname !== 'string') return console.log('ERROR:  Invalid dirname');
else console.log('INFO:  Using directory: ' + dirname);

// Warn of includedirs
if (includedirs) console.log('WARNING:  Including directories in search');
else console.log('INFO:  Not including directories in search');

// Check if searchfor is valid
if (typeof searchfor !== 'string' && !method.includes('numbered')) return console.log('ERROR:  Invalid searchfor');
if (typeof searchfor !== 'string' && method.includes('numbered')) console.log('WARNING:  Searchfor is undefined, this will replace the file name with a number');
else console.log(`INFO:  Searching for: ${searchfor}`);

// Warn if replace is undefined
if (replace === undefined) console.log('WARNING:  Replace is undefined, this will remove the searchfor from the file name entirely');
else console.log(`INFO:  Replacing with: ${replace}`);

// Tell user if startingnumber is not 0
if (startingnumber !== 0) console.log(`INFO:  Starting number: ${startingnumber}`);

// Check if debuglog is enabled
if (debuglog) console.log('INFO:  Debug log is enabled');
else console.log('INFO:  Debug log is disabled');


console.log('\n');
console.log('INFO:  Starting search...');
console.log('\n');


const fs = require('fs');
const files = fs.readdirSync(dirname);
let number = 0 + startingnumber;


// Debug log
if (debuglog) {
	console.log('DEBUG:  Files:');
	console.log(files);
	console.log(`DEBUG:  Files type: ${typeof files}`);
	console.log('\n');
}


files.forEach((file) => {
	// Split file name and extension
	// If there is no extension, name[1] will be a blank string to avoid files getting fucked up
	let name = file.includes('.') ? file.split('.') : [file, ''];
	name.reverse();
	if (name[0].length > 0) name[0] = '.' + name[0];
	var extension = name[0];
	name.reverse();
	if (removeextraperiods) name = [name.join('').slice(0, -extension.length), extension];
	else name = [name.join('.').slice(0, -extension.length), extension];


	// Check if file is this script
	if (process.argv[1].includes(name.join(''))) return;


	// Debug log
	if (debuglog) console.log(`DEBUG:  File name: ${name[0]}`);


	// Check if file is a directory
	if (!includedirs) {
		try {
			if (fs.readdirSync(dirname + file).length) {
				if (debuglog) console.log(`DEBUG:  File is a directory, skipping: ${name[0]}`);
				if (debuglog) console.log('\n');
				return;
			}
		} catch (e) {
			if (debuglog) console.log(`DEBUG:  File is a file: ${name[0]}`);
		}
	} else {
		try {
			if (fs.readdirSync(dirname + file).length) {
				if (debuglog) console.log(`DEBUG:  File is a directory: ${name[0]}`);
			}
		} catch (e) {
			if (debuglog) console.log(`DEBUG:  File is a file: ${name[0]}`);
		}
	}


	if (method === 'startsWith') {
		if (!name[0].startsWith(searchfor)) {
			if (debuglog) console.log(`DEBUG:  File name does not start with '${searchfor}': ${name[0]}`);
			if (debuglog) console.log('\n');
			return;
		}

		if (replace) {
			fs.renameSync(dirname + '/' + file, dirname + '/' + replace + name[0].slice(searchfor.length) + name[1]);
		} else {
			fs.renameSync(dirname + '/' + file, dirname + '/' + name[0].slice(searchfor.length) + name[1]);
		}
	} else if (method === 'endsWith') {
		if (!name[0].endsWith(searchfor)) {
			if (debuglog) console.log(`DEBUG:  File name does not end with '${searchfor}': ${name[0]}`);
			if (debuglog) console.log('\n');
			return;
		}

		if (replace) {
			fs.renameSync(dirname + '/' + file, dirname + '/' + name[0].slice(0, -searchfor.length) + replace + name[1]);
		} else {
			fs.renameSync(dirname + '/' + file, dirname + '/' + name[0].slice(0, -searchfor.length) + name[1]);
		}
	} else if (method === 'includes') {
		if (!name[0].includes(searchfor)) {
			if (debuglog) console.log(`DEBUG:  File name does not include '${searchfor}': ${name[0]}`);
			if (debuglog) console.log('\n');
			return;
		}

		const regex = new RegExp(searchfor, 'g');

		if (replace) {
			fs.renameSync(dirname + '/' + file, dirname + '/' + name[0].replace(regex, replace) + name[1]);
		} else {
			fs.renameSync(dirname + '/' + file, dirname + '/' + name[0].replace(regex, '') + name[1]);
		}
	} else if (method === 'numberedPrefix') {
		if (!name[0].startsWith(searchfor)) {
			if (debuglog) console.log(`DEBUG:  File name does not start with '${searchfor}': ${name[0]}`);
			if (debuglog) console.log('\n');
			return;
		}

		fs.renameSync(dirname + '/' + file, dirname + '/' + number + name[0] + name[1]);

		number++;
	} else if (method === 'numberedSuffix') {
		if (!name[0].endsWith(searchfor)) {
			if (debuglog) console.log(`DEBUG:  File name does not end with '${searchfor}': ${name[0]}`);
			if (debuglog) console.log('\n');
			return;
		}

		fs.renameSync(dirname + '/' + file, dirname + '/' + name[0] + number + name[1]);

		number++;
	} else if (method === 'numbered') {
		if (typeof searchfor === 'string') {
			if (!name[0].includes(searchfor)) {
				if (debuglog) console.log(`DEBUG:  File name does not include '${searchfor}': ${name[0]}`);
				if (debuglog) console.log('\n');
				return;
			}

			fs.renameSync(dirname + '/' + file, dirname + '/' + number + name[1]);
		} else {
			fs.renameSync(dirname + '/' + file, dirname + '/' + number + name[1]);
		}

		number++;
	}

	console.log('\n');
});

console.log('INFO:  Search complete');
