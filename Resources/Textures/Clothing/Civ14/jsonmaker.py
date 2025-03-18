import os
import shutil

currdir = os.getcwd()

prefixlist = ["inhand-left-","inhand-right-","equipped-INNERCLOTHING-","equipped-FEET-","equipped-BACKPACK-","equipped-BELT-","equipped-EYES-","equipped-HAND-","equipped-HELMET-","equipped-MASK-","equipped-NECK-","equipped-OUTERCLOTHING-"]

if (__name__ == "__main__"):
	print("Checking JSON...")
	for root, dirs, files in os.walk(currdir+"\\exported"): # checks all files and folders in the base folder
		for file in files:
			filesp = file.replace("\n","") # removes the paragraph at the end of the string
			if(file.endswith(".png")):
				if not os.path.isfile(root+"\\meta.json"):
					print("    Not found at {}, creating!".format(root))
					with open(root+"\\meta.json", "w") as newmeta:
						newmeta.write('''{\"version\": 1,\"copyright\": \"Taken from civ13 at commit https://github.com/Civ13/Civ13/commit/c07b37fbca55b690d80cc2ec0c2c61839cbecf5c\",\"size\": {\"x\": 32,\"y\": 32},
		\"states\": [''')
						newmeta.close()
				dirs = ''
				for prefix in prefixlist:
					if file.find(prefix):
						dirs = ',\"directions\": 4'
				jsonstr = '{"name": "'+file.replace(".png","")+'"'+dirs+'},'
				print("    Adding".format(jsonstr))
				with open(root+"\\meta.json", "a") as text_file:
					text_file.write(jsonstr)
				text_file.close()
	for root, dirs, files in os.walk(currdir+"\\exported"): # checks all files and folders in the base folder
		for file in files:
			filesp = file.replace("\n","") # removes the paragraph at the end of the string
			if(file.endswith(".json")):
				print("finalising " + root + "/"+file)
				with open(root+"\\meta.json", "a") as final_file:
					final_file.write("]}")
