import os
import shutil

currdir = os.getcwd()
path = currdir
prefixlist = ["icon-","inhand-left-","inhand-right-","equipped-INNERCLOTHING-","equipped-FEET-","equipped-BACKPACK-","equipped-BELT-","equipped-EYES-","equipped-HAND-","equipped-HELMET-","equipped-MASK-","equipped-NECK-","equipped-OUTERCLOTHING-"]

if (__name__ == "__main__"):
	print("Listing files...")
	with open("fulllist.txt","w") as writing: # this is where we will list all the orphaned files
		for root, dirs, files in os.walk(path+"\\sprites"): # checks all files and folders in the base folder
			for file in files:
				filesp = file.replace("\n","") # removes the paragraph at the end of the string
				if(file.endswith(".png") and filesp.find("exported") == -1):
					# if it has one of the extensions, split it so we get the filename without dirs
					filesp = str(root)+"\\"+str(file) # get the absolute directory
					_id = file.replace(".png","")
					equip = ""
					for prefix in prefixlist:
						if _id.find(prefix) != -1:
							_name,_id = _id.split(prefix)
							if prefix.find("equipped-"):
								equip = prefix
					writing.write(filesp+"||"+_id+"||"+equip+"\n") # return the last value of the splitted array and write to the file
	#all listed, now lets pair
	print("Finished listing the files.")
	print("Pairing by id...")
	if not os.path.isdir(currdir+"\\exported"):
		os.mkdir(currdir+"\\exported")
	with open("fulllist.txt", "r") as reading:
		for filepath in reading:
			filepath = filepath.replace("\n","") # remove the paragraph
			splitpath,splitid,splitprefix = filepath.split("||")
			print("Checking {}...".format(splitid))
			with open("fulllist.txt", "r") as reading2:
				for filepath2 in reading2:
					filepath2 = filepath2.replace("\n","") # remove the paragraph
					if not filepath2 == filepath:
						splitpath2,splitid2,splitprefix2 = filepath2.split("||")
						if splitid == splitid2 and not splitpath == splitpath2:
							print("   {} matches {}!".format(splitpath2,splitid))
							if not os.path.isdir(currdir+"\\exported\\"+splitid):
								os.mkdir(currdir+"\\exported\\"+splitid)
							shutil.copy(splitpath, currdir+"\\exported\\"+splitid)
							shutil.copy(splitpath2, currdir+"\\exported\\"+splitid)
							
	reading.close()
