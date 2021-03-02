#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Maths;

namespace Content.Shared.Preferences.Appearance
{
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public static class HairStyles
    {
        public const string DefaultHairStyle = "Bald";
        public const string DefaultFacialHairStyle = "Shaved";

        public static readonly Dictionary<string, string> HairStylesMap = new()
        {
            {"Afro", "afro"},
            {"Afro 2", "afro2"},
            {"Afro (Large)", "bigafro"},
            {"Ahoge", "antenna"},
            {"Bald", "bald"},
            {"Balding Hair", "e"},
            {"Bedhead", "bedhead"},
            {"Bedhead 2", "bedheadv2"},
            {"Bedhead 3", "bedheadv3"},
            {"Long Bedhead", "long_bedhead"},
            {"Floorlength Bedhead", "floorlength_bedhead"},
            {"Beehive", "beehive"},
            {"Beehive 2", "beehivev2"},
            {"Bob Hair", "bob"},
            {"Bob Hair 2", "bob2"},
            {"Bob Hair 3", "bobcut"},
            {"Bob Hair 4", "bob4"},
            {"Bobcurl", "bobcurl"},
            {"Boddicker", "boddicker"},
            {"Bowlcut", "bowlcut"},
            {"Bowlcut 2", "bowlcut2"},
            {"Braid (Floorlength)", "braid"},
            {"Braided", "braided"},
            {"Braided Front", "braidfront"},
            {"Braid (High)", "braid2"},
            {"Braid (Low)", "hbraid"},
            {"Braid (Short)", "shortbraid"},
            {"Braided Tail", "braidtail"},
            {"Bun Head", "bun"},
            {"Bun Head 2", "bunhead2"},
            {"Bun Head 3", "bun3"},
            {"Bun (Large)", "largebun"},
            {"Bun (Manbun)", "manbun"},
            {"Bun (Tight)", "tightbun"},
            {"Business Hair", "business"},
            {"Business Hair 2", "business2"},
            {"Business Hair 3", "business3"},
            {"Business Hair 4", "business4"},
            {"Buzzcut", "buzzcut"},
            {"CIA", "cia"},
            {"Coffee House", "coffeehouse"},
            {"Combover", "combover"},
            {"Cornrows", "cornrows"},
            {"Cornrows 2", "cornrows2"},
            {"Cornrow Bun", "cornrowbun"},
            {"Cornrow Braid", "cornrowbraid"},
            {"Cornrow Tail", "cornrowtail"},
            {"Crewcut", "crewcut"},
            {"Curls", "curls"},
            {"Cut Hair", "c"},
            {"Dandy Pompadour", "dandypompadour"},
            {"Devil Lock", "devilock"},
            {"Double Bun", "doublebun"},
            {"Dreadlocks", "dreads"},
            {"Drillruru", "drillruru"},
            {"Drill Hair (Extended)", "drillhairextended"},
            {"Emo", "emo"},
            {"Emo Fringe", "emofringe"},
            {"Fade (None)", "nofade"},
            {"Fade (High)", "highfade"},
            {"Fade (Medium)", "medfade"},
            {"Fade (Low)", "lowfade"},
            {"Fade (Bald)", "baldfade"},
            {"Feather", "feather"},
            {"Father", "father"},
            {"Flat Top", "sargeant"},
            {"Flair", "flair"},
            {"Flat Top (Big)", "bigflattop"},
            {"Flow Hair", "f"},
            {"Gelled Back", "gelled"},
            {"Gentle", "gentle"},
            {"Half-banged Hair", "halfbang"},
            {"Half-banged Hair 2", "halfbang2"},
            {"Half-shaved", "halfshaved"},
            {"Hedgehog Hair", "hedgehog"},
            {"Hime Cut", "himecut"},
            {"Hime Cut 2", "himecut2"},
            {"Hime Cut (Short)", "shorthime"},
            {"Hime Updo", "himeup"},
            {"Hitop", "hitop"},
            {"Jade", "jade"},
            {"Jensen Hair", "jensen"},
            {"Joestar", "joestar"},
            {"Keanu Hair", "keanu"},
            {"Kusanagi Hair", "kusanagi"},
            {"Long Hair 1", "long"},
            {"Long Hair 2", "long2"},
            {"Long Hair 3", "long3"},
            {"Long Over Eye", "longovereye"},
            {"Long Bangs", "lbangs"},
            {"Long Emo", "longemo"},
            {"Long Fringe", "longfringe"},
            {"Long Side Part", "longsidepart"},
            {"Mega Eyebrows", "megaeyebrows"},
            {"Messy", "messy"},
            {"Modern", "modern"},
            {"Mohawk", "d"},
            {"Nitori", "nitori"},
            {"Mohawk (Reverse)", "reversemohawk"},
            {"Mohawk (Unshaven)", "unshaven_mohawk"},
            {"Mulder", "mulder"},
            {"Odango", "odango"},
            {"Ombre", "ombre"},
            {"One Shoulder", "oneshoulder"},
            {"Over Eye", "shortovereye"},
            {"Oxton", "oxton"},
            {"Parted", "parted"},
            {"Parted (Side)", "part"},
            {"Pigtails", "kagami"},
            {"Pigtails 2", "pigtails"},
            {"Pigtails 3", "pigtails2"},
            {"Pixie Cut", "pixie"},
            {"Pompadour", "pompadour"},
            {"Pompadour (Big)", "bigpompadour"},
            {"Ponytail", "ponytail"},
            {"Ponytail 2", "ponytail2"},
            {"Ponytail 3", "ponytail3"},
            {"Ponytail 4", "ponytail4"},
            {"Ponytail 5", "ponytail5"},
            {"Ponytail 6", "ponytail6"},
            {"Ponytail 7", "ponytail7"},
            {"Ponytail (High)", "highponytail"},
            {"Ponytail (Short)", "stail"},
            {"Ponytail (Long)", "longstraightponytail"},
            {"Ponytail (Country)", "country"},
            {"Ponytail (Fringe)", "fringetail"},
            {"Ponytail (Side)", "sidetail"},
            {"Ponytail (Side) 2", "sidetail2"},
            {"Ponytail (Side) 3", "sidetail3"},
            {"Ponytail (Side) 4", "sidetail4"},
            {"Ponytail (Spiky)", "spikyponytail"},
            {"Poofy", "poofy"},
            {"Quiff", "quiff"},
            {"Ronin", "ronin"},
            {"Shaved", "shaved"},
            {"Shaved Part", "shavedpart"},
            {"Short Bangs", "shortbangs"},
            {"Short Hair", "a"},
            {"Short Hair 2", "shorthair2"},
            {"Short Hair 3", "shorthair3"},
            {"Short Hair 4", "d"},
            {"Short Hair 5", "e"},
            {"Short Hair 6", "f"},
            {"Short Hair 7", "shorthairg"},
            {"Short Hair 80s", "80s"},
            {"Short Hair Rosa", "rosa"},
            {"Shoulder-length Hair", "b"},
            {"Sidecut", "sidecut"},
            {"Skinhead", "skinhead"},
            {"Slightly Long Hair", "protagonist"},
            {"Spiky", "spikey"},
            {"Spiky 2", "spiky"},
            {"Spiky 3", "spiky2"},
            {"Swept Back Hair", "swept"},
            {"Swept Back Hair 2", "swept2"},
            {"Thinning", "thinning"},
            {"Thinning (Front)", "thinningfront"},
            {"Thinning (Rear)", "thinningrear"},
            {"Topknot", "topknot"},
            {"Tress Shoulder", "tressshoulder"},
            {"Trimmed", "trimmed"},
            {"Trim Flat", "trimflat"},
            {"Twintails", "twintail"},
            {"Undercut", "undercut"},
            {"Undercut Left", "undercutleft"},
            {"Undercut Right", "undercutright"},
            {"Unkept", "unkept"},
            {"Updo", "updo"},
            {"Very Long Hair", "vlong"},
            {"Very Long Hair 2", "longest"},
            {"Very Long Over Eye", "longest2"},
            {"Very Short Over Eye", "veryshortovereyealternate"},
            {"Very Long with Fringe", "vlongfringe"},
            {"Volaju", "volaju"},
            {"Wisp", "wisp"},
        };

        public static readonly Dictionary<string, string> FacialHairStylesMap = new()
        {
            {"Beard (Abraham Lincoln)", "abe"},
            {"Beard (Broken Man)", "brokenman"},
            {"Beard (Chinstrap)", "chin"},
            {"Beard (Dwarf)", "dwarf"},
            {"Beard (Full)", "fullbeard"},
            {"Beard (Cropped Fullbeard)", "croppedfullbeard"},
            {"Beard (Goatee)", "gt"},
            {"Beard (Hipster)", "hip"},
            {"Beard (Jensen)", "jensen"},
            {"Beard (Neckbeard)", "neckbeard"},
            {"Beard (Very Long)", "wise"},
            {"Beard (Muttonmus)", "muttonmus"},
            {"Beard (Martial Artist)", "martialartist"},
            {"Beard (Chinless Beard)", "chinlessbeard"},
            {"Beard (Moonshiner)", "moonshiner"},
            {"Beard (Long)", "longbeard"},
            {"Beard (Volaju)", "volaju"},
            {"Beard (Three o Clock Shadow)", "3oclock"},
            {"Beard (Five o Clock Shadow)", "fiveoclock"},
            {"Beard (Five o Clock Moustache)", "5oclockmoustache"},
            {"Beard (Seven o Clock Shadow)", "7oclock"},
            {"Beard (Seven o Clock Moustache)", "7oclockmoustache"},
            {"Moustache", "moustache"},
            {"Moustache (Pencilstache)", "pencilstache"},
            {"Moustache (Smallstache)", "smallstache"},
            {"Moustache (Walrus)", "walrus"},
            {"Moustache (Fu Manchu)", "fumanchu"},
            {"Moustache (Hulk Hogan)", "hogan"},
            {"Moustache (Selleck)", "selleck"},
            {"Moustache (Square)", "chaplin"},
            {"Moustache (Van Dyke)", "vandyke"},
            {"Moustache (Watson)", "watson"},
            {"Sideburns (Elvis)", "elvis"},
            {"Sideburns (Mutton Chops)", "mutton"},
            {"Sideburns", "sideburn"},
            {"Shaved", "shaved"}
        };

        // These comparers put the default hair style (shaved/bald) at the very top.
        // For in the hair style pickers.

        public static readonly IComparer<KeyValuePair<string, string>> HairStyleComparer =
            Comparer<KeyValuePair<string, string>>.Create((a, b) =>
            {
                var styleA = a.Key;
                var styleB = b.Key;
                if (styleA == DefaultHairStyle)
                {
                    return -1;
                }

                if (styleB == DefaultHairStyle)
                {
                    return 1;
                }

                return string.Compare(styleA, styleB, StringComparison.CurrentCulture);
            });

        public static readonly IComparer<KeyValuePair<string, string>> FacialHairStyleComparer =
            Comparer<KeyValuePair<string, string>>.Create((a, b) =>
            {
                var styleA = a.Key;
                var styleB = b.Key;

                if (styleA == DefaultFacialHairStyle)
                {
                    return -1;
                }

                if (styleB == DefaultFacialHairStyle)
                {
                    return 1;
                }

                return string.Compare(styleA, styleB, StringComparison.CurrentCulture);
            });

        public static IReadOnlyList<Color> RealisticHairColors = new List<Color>
        {
            Color.Yellow,
            Color.Black,
            Color.SandyBrown,
            Color.Brown,
            Color.Wheat,
            Color.Gray
        };
    }
}
