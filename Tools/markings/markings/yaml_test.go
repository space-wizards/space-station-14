package markings

import (
	"bytes"
	"gopkg.in/yaml.v3"
	"testing"
)

func TestPrototypeUnmarshal(t *testing.T) {
	data := `
        - categories: test
          id: test
          sprite:
            sprite: test
            state: test
          type: spriteAccessory
    `

	var accessories []SpriteAccessoryPrototype

	err := yaml.Unmarshal([]byte(data), &accessories)
	if err != nil {
		t.Fatal(err)
	}

	accessory := accessories[0]

	if accessory.Categories != "test" || accessory.Id != "test" || accessory.Type != "spriteAccessory" {
		t.Fatal("incorrect unmarshal, accessory:", accessory)
	}

	if accessory.Sprite.Sprite != "test" || accessory.Sprite.State != "test" {
		t.Fatal("incorrect unmarshal", accessory)
	}
}

func TestLoadFromYaml(t *testing.T) {
	data := `
        - categories: test
          id: test
          sprite:
            sprite: test
            state: test
          type: spriteAccessory
    `

	b := bytes.NewBufferString(data)

	accessories, err := loadFromYaml[SpriteAccessoryPrototype](b)
	if err != nil {
		t.Fatal(err)
	}

	accessory := accessories[0]

	if accessory.Categories != "test" || accessory.Id != "test" || accessory.Type != "spriteAccessory" {
		t.Fatal("incorrect unmarshal, accessory:", accessory)
	}

	if accessory.Sprite.Sprite != "test" || accessory.Sprite.State != "test" {
		t.Fatal("incorrect unmarshal", accessory)
	}
}
