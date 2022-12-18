package markings

import (
	"flag"
	"fmt"
	"os"
	"path/filepath"
)

// i don't necessarily care

func Entry() {
	wd, err := os.Getwd()
	if err != nil {
		panic(err)
	}

	flag.Parse()
	args := flag.Args()
	if len(args) != 3 {
		fmt.Println("usage: [command] [in] [out]\nvalid commands: convertAccessories")
		os.Exit(1)
	}
	command := args[0]
	in := filepath.Join(wd, args[1])

	out := filepath.Join(wd, args[2])

	switch command {
	case "convertAccessories":
		file, err := os.Open(in)
		if err != nil {
			panic(err)
		}

		accessories, err := loadFromYaml[SpriteAccessoryPrototype](file)

		markings, err := accessories_to_markings(accessories)
		if err != nil {
			panic(err)
		}

		err = saveToYaml(markings, out)
		if err != nil {
			panic(err)
		}
	}
}
