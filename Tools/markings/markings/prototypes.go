package markings

var accessoryLayerMapping map[string]string

const (
	// Prototype type.
	Marking = "marking"

	// Marking categories/humanoid visual layers.
	Hair       = "Hair"
	FacialHair = "FacialHair"

	// SpriteAccessory categories.
	HumanHair       = "HumanHair"
	HumanFacialHair = "HumanFacialHair"
	VoxFacialHair   = "VoxFacialHair"
	VoxHair         = "VoxHair"
    SpElfHair       = "SpElfHair"
)

func init() {
	accessoryLayerMapping = make(map[string]string)
	accessoryLayerMapping[HumanHair] = Hair
	accessoryLayerMapping[HumanFacialHair] = FacialHair
	accessoryLayerMapping[VoxFacialHair] = FacialHair
	accessoryLayerMapping[VoxHair] = Hair
    accessoryLayerMapping[SpElfHair] = Hair
}

type SpriteAccessoryPrototype struct {
	Type       string          `yaml:"type"`
	Categories string          `yaml:"categories"`
	Id         string          `yaml:"id"`
	Sprite     SpriteSpecifier `yaml:"sprite"`
}

func (s *SpriteAccessoryPrototype) toMarking() (*MarkingPrototype, error) {
	sprites := []SpriteSpecifier{s.Sprite}

	var category string
	category = accessoryLayerMapping[s.Categories]
	// isMatching := false
	/*
		for _, v := range s.Categories {
			if len(category) == 0 {
				category = accessoryLayerMapping[v]
			}

			isMatching = accessoryLayerMapping[v] == category

			if !isMatching {
				return nil, errors.New("sprite accessory prototype has differing accessory categories")
			}
		}
	*/

	return &MarkingPrototype{
		Type:            Marking,
		Id:              s.Id,
		BodyPart:        category,
		MarkingCategory: category,
		Sprites:         sprites,
	}, nil
}

type MarkingPrototype struct {
	Type               string            `yaml:"type"`
	Id                 string            `yaml:"id"`
	BodyPart           string            `yaml:"bodyPart"`
	MarkingCategory    string            `yaml:"markingCategory"`
	SpeciesRestriction []string          `yaml:"speciesRestriction,omitempty"`
	Sprites            []SpriteSpecifier `yaml:"sprites"`
	Shader             string?           `yaml:"shader"`
}

type SpriteSpecifier struct {
	Sprite string `yaml:"sprite"`
	State  string `yaml:"state"`
}
