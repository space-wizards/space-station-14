package markings

import (
	"gopkg.in/yaml.v3"
	"io"
	"os"
)

// boilerplate-y but that's just go in a nutshell

// FINALLY SOME FUCKING GENERICS

func loadFromYaml[T any](file io.Reader) ([]T, error) {
	var res []T

	bytes, err := io.ReadAll(file)
	if err != nil {
		return nil, err
	}

	err = yaml.Unmarshal(bytes, &res)
	if err != nil {
		return nil, err
	}

	return res, nil
}

func saveToYaml[T any](prototype []T, filePath string) error {
	file, err := os.Create(filePath)
	if err != nil {
		return err
	}

	bytes, err := yaml.Marshal(prototype)
	if err != nil {
		return err
	}

	_, err = file.Write(bytes)
	if err != nil {
		return err
	}

	return nil
}
