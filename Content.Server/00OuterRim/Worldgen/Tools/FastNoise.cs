// FastNoise.cs
//
// MIT License
//
// Copyright(c) 2017 Jordan Peck
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//
// The developer's email is jorzixdan.me2@gzixmail.com (for great email, take
// off every 'zix'.)
//

// Uncomment the line below to swap all the inputs/outputs/calculations of FastNoise to doubles instead of floats
//#define FN_USE_DOUBLES

#nullable disable

#if FN_USE_DOUBLES
using FN_DECIMAL = System.Double;
#else
using FN_DECIMAL = System.Single;
#endif
using System.Runtime.CompilerServices;

namespace Content.Server._00OuterRim.Worldgen.Tools;

public class FastNoise
{
    private const MethodImplOptions FN_INLINE = MethodImplOptions.AggressiveInlining;
    private const int FN_CELLULAR_INDEX_MAX = 3;

    public enum NoiseType : byte
    {
        Value,
        ValueFractal,
        Perlin,
        PerlinFractal,
        Simplex,
        SimplexFractal,
        Cellular,
        WhiteNoise,
        Cubic,
        CubicFractal
    };

    public enum Interp : byte
    {
        Linear,
        Hermite,
        Quintic
    };

    public enum FractalType : byte
    {
        FBM,
        Billow,
        RigidMulti
    };

    public enum CellularDistanceFunction : byte
    {
        Euclidean,
        Manhattan,
        Natural
    };

    public enum CellularReturnType : byte
    {
        CellValue,
        NoiseLookup,
        Distance,
        Distance2,
        Distance2Add,
        Distance2Sub,
        Distance2Mul,
        Distance2Div
    };

    private int m_seed = 1337;
    private FN_DECIMAL m_frequency = (FN_DECIMAL) 0.01;
    private Interp m_interp = Interp.Quintic;
    private NoiseType m_noiseType = NoiseType.Simplex;

    private int m_octaves = 3;
    private FN_DECIMAL m_lacunarity = (FN_DECIMAL) 2.0;
    private FN_DECIMAL m_gain = (FN_DECIMAL) 0.5;
    private FractalType m_fractalType = FractalType.FBM;

    private FN_DECIMAL m_fractalBounding;

    private CellularDistanceFunction m_cellularDistanceFunction = CellularDistanceFunction.Euclidean;
    private CellularReturnType m_cellularReturnType = CellularReturnType.CellValue;
    private FastNoise m_cellularNoiseLookup = null;
    private int m_cellularDistanceIndex0 = 0;
    private int m_cellularDistanceIndex1 = 1;
    private float m_cellularJitter = 0.45f;

    private FN_DECIMAL m_gradientPerturbAmp = (FN_DECIMAL) 1.0;

    public FastNoise(int seed = 1337)
    {
        m_seed = seed;
        CalculateFractalBounding();
    }

    // Returns a 0 float/double
    public static FN_DECIMAL GetDecimalType()
    {
        return 0;
    }

    // Returns the seed used by this object
    public int GetSeed()
    {
        return m_seed;
    }

    // Sets seed used for all noise types
    // Default: 1337
    public void SetSeed(int seed)
    {
        m_seed = seed;
    }

    // Sets frequency for all noise types
    // Default: 0.01
    public void SetFrequency(FN_DECIMAL frequency)
    {
        m_frequency = frequency;
    }

    // Changes the interpolation method used to smooth between noise values
    // Possible interpolation methods (lowest to highest quality) :
    // - Linear
    // - Hermite
    // - Quintic
    // Used in Value, Gradient Noise and Position Perturbing
    // Default: Quintic
    public void SetInterp(Interp interp)
    {
        m_interp = interp;
    }

    // Sets noise return type of GetNoise(...)
    // Default: Simplex
    public void SetNoiseType(NoiseType noiseType)
    {
        m_noiseType = noiseType;
    }


    // Sets octave count for all fractal noise types
    // Default: 3
    public void SetFractalOctaves(int octaves)
    {
        m_octaves = octaves;
        CalculateFractalBounding();
    }

    // Sets octave lacunarity for all fractal noise types
    // Default: 2.0
    public void SetFractalLacunarity(FN_DECIMAL lacunarity)
    {
        m_lacunarity = lacunarity;
    }

    // Sets octave gain for all fractal noise types
    // Default: 0.5
    public void SetFractalGain(FN_DECIMAL gain)
    {
        m_gain = gain;
        CalculateFractalBounding();
    }

    // Sets method for combining octaves in all fractal noise types
    // Default: FBM
    public void SetFractalType(FractalType fractalType)
    {
        m_fractalType = fractalType;
    }


    // Sets return type from cellular noise calculations
    // Note: NoiseLookup requires another FastNoise object be set with SetCellularNoiseLookup() to function
    // Default: CellValue
    public void SetCellularDistanceFunction(CellularDistanceFunction cellularDistanceFunction)
    {
        m_cellularDistanceFunction = cellularDistanceFunction;
    }

    // Sets distance function used in cellular noise calculations
    // Default: Euclidean
    public void SetCellularReturnType(CellularReturnType cellularReturnType)
    {
        m_cellularReturnType = cellularReturnType;
    }

    // Sets the 2 distance indicies used for distance2 return types
    // Default: 0, 1
    // Note: index0 should be lower than index1
    // Both indicies must be >= 0, index1 must be < 4
    public void SetCellularDistance2Indicies(int cellularDistanceIndex0, int cellularDistanceIndex1)
    {
        m_cellularDistanceIndex0 = Math.Min(cellularDistanceIndex0, cellularDistanceIndex1);
        m_cellularDistanceIndex1 = Math.Max(cellularDistanceIndex0, cellularDistanceIndex1);

        m_cellularDistanceIndex0 = Math.Min(Math.Max(m_cellularDistanceIndex0, 0), FN_CELLULAR_INDEX_MAX);
        m_cellularDistanceIndex1 = Math.Min(Math.Max(m_cellularDistanceIndex1, 0), FN_CELLULAR_INDEX_MAX);
    }

    // Sets the maximum distance a cellular point can move from it's grid position
    // Setting this high will make artifacts more common
    // Default: 0.45
    public void SetCellularJitter(float cellularJitter)
    {
        m_cellularJitter = cellularJitter;
    }

    // Noise used to calculate a cell value if cellular return type is NoiseLookup
    // The lookup value is acquired through GetNoise() so ensure you SetNoiseType() on the noise lookup, value, gradient or simplex is recommended
    public void SetCellularNoiseLookup(FastNoise noise)
    {
        m_cellularNoiseLookup = noise;
    }


    // Sets the maximum perturb distance from original location when using GradientPerturb{Fractal}(...)
    // Default: 1.0
    public void SetGradientPerturbAmp(FN_DECIMAL gradientPerturbAmp)
    {
        m_gradientPerturbAmp = gradientPerturbAmp;
    }

    private struct Float2
    {
        public readonly FN_DECIMAL x, y;

        public Float2(FN_DECIMAL x, FN_DECIMAL y)
        {
            this.x = x;
            this.y = y;
        }
    }

    private struct Float3
    {
        public readonly FN_DECIMAL x, y, z;

        public Float3(FN_DECIMAL x, FN_DECIMAL y, FN_DECIMAL z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
    }

    private static readonly Float2[] GRAD_2D =
    {
        new(-1, -1), new(1, -1), new(-1, 1), new(1, 1),
        new(0, -1), new(-1, 0), new(0, 1), new(1, 0),
    };

    private static readonly Float3[] GRAD_3D =
    {
        new(1, 1, 0), new(-1, 1, 0), new(1, -1, 0), new(-1, -1, 0),
        new(1, 0, 1), new(-1, 0, 1), new(1, 0, -1), new(-1, 0, -1),
        new(0, 1, 1), new(0, -1, 1), new(0, 1, -1), new(0, -1, -1),
        new(1, 1, 0), new(0, -1, 1), new(-1, 1, 0), new(0, -1, -1),
    };

    private static readonly Float2[] CELL_2D =
    {
        new(-0.2700222198f, -0.9628540911f), new(0.3863092627f, -0.9223693152f),
        new(0.04444859006f, -0.999011673f), new(-0.5992523158f, -0.8005602176f),
        new(-0.7819280288f, 0.6233687174f), new(0.9464672271f, 0.3227999196f),
        new(-0.6514146797f, -0.7587218957f), new(0.9378472289f, 0.347048376f),
        new(-0.8497875957f, -0.5271252623f), new(-0.879042592f, 0.4767432447f),
        new(-0.892300288f, -0.4514423508f), new(-0.379844434f, -0.9250503802f),
        new(-0.9951650832f, 0.0982163789f), new(0.7724397808f, -0.6350880136f),
        new(0.7573283322f, -0.6530343002f), new(-0.9928004525f, -0.119780055f),
        new(-0.0532665713f, 0.9985803285f), new(0.9754253726f, -0.2203300762f),
        new(-0.7665018163f, 0.6422421394f), new(0.991636706f, 0.1290606184f),
        new(-0.994696838f, 0.1028503788f), new(-0.5379205513f, -0.84299554f),
        new(0.5022815471f, -0.8647041387f), new(0.4559821461f, -0.8899889226f),
        new(-0.8659131224f, -0.5001944266f), new(0.0879458407f, -0.9961252577f),
        new(-0.5051684983f, 0.8630207346f), new(0.7753185226f, -0.6315704146f),
        new(-0.6921944612f, 0.7217110418f), new(-0.5191659449f, -0.8546734591f),
        new(0.8978622882f, -0.4402764035f), new(-0.1706774107f, 0.9853269617f),
        new(-0.9353430106f, -0.3537420705f), new(-0.9992404798f, 0.03896746794f),
        new(-0.2882064021f, -0.9575683108f), new(-0.9663811329f, 0.2571137995f),
        new(-0.8759714238f, -0.4823630009f), new(-0.8303123018f, -0.5572983775f),
        new(0.05110133755f, -0.9986934731f), new(-0.8558373281f, -0.5172450752f),
        new(0.09887025282f, 0.9951003332f), new(0.9189016087f, 0.3944867976f),
        new(-0.2439375892f, -0.9697909324f), new(-0.8121409387f, -0.5834613061f),
        new(-0.9910431363f, 0.1335421355f), new(0.8492423985f, -0.5280031709f),
        new(-0.9717838994f, -0.2358729591f), new(0.9949457207f, 0.1004142068f),
        new(0.6241065508f, -0.7813392434f), new(0.662910307f, 0.7486988212f),
        new(-0.7197418176f, 0.6942418282f), new(-0.8143370775f, -0.5803922158f),
        new(0.104521054f, -0.9945226741f), new(-0.1065926113f, -0.9943027784f),
        new(0.445799684f, -0.8951327509f), new(0.105547406f, 0.9944142724f),
        new(-0.992790267f, 0.1198644477f), new(-0.8334366408f, 0.552615025f),
        new(0.9115561563f, -0.4111755999f), new(0.8285544909f, -0.5599084351f),
        new(0.7217097654f, -0.6921957921f), new(0.4940492677f, -0.8694339084f),
        new(-0.3652321272f, -0.9309164803f), new(-0.9696606758f, 0.2444548501f),
        new(0.08925509731f, -0.996008799f), new(0.5354071276f, -0.8445941083f),
        new(-0.1053576186f, 0.9944343981f), new(-0.9890284586f, 0.1477251101f),
        new(0.004856104961f, 0.9999882091f), new(0.9885598478f, 0.1508291331f),
        new(0.9286129562f, -0.3710498316f), new(-0.5832393863f, -0.8123003252f),
        new(0.3015207509f, 0.9534596146f), new(-0.9575110528f, 0.2883965738f),
        new(0.9715802154f, -0.2367105511f), new(0.229981792f, 0.9731949318f),
        new(0.955763816f, -0.2941352207f), new(0.740956116f, 0.6715534485f),
        new(-0.9971513787f, -0.07542630764f), new(0.6905710663f, -0.7232645452f),
        new(-0.290713703f, -0.9568100872f), new(0.5912777791f, -0.8064679708f),
        new(-0.9454592212f, -0.325740481f), new(0.6664455681f, 0.74555369f),
        new(0.6236134912f, 0.7817328275f), new(0.9126993851f, -0.4086316587f),
        new(-0.8191762011f, 0.5735419353f), new(-0.8812745759f, -0.4726046147f),
        new(0.9953313627f, 0.09651672651f), new(0.9855650846f, -0.1692969699f),
        new(-0.8495980887f, 0.5274306472f), new(0.6174853946f, -0.7865823463f),
        new(0.8508156371f, 0.52546432f), new(0.9985032451f, -0.05469249926f),
        new(0.1971371563f, -0.9803759185f), new(0.6607855748f, -0.7505747292f),
        new(-0.03097494063f, 0.9995201614f), new(-0.6731660801f, 0.739491331f),
        new(-0.7195018362f, -0.6944905383f), new(0.9727511689f, 0.2318515979f),
        new(0.9997059088f, -0.0242506907f), new(0.4421787429f, -0.8969269532f),
        new(0.9981350961f, -0.061043673f), new(-0.9173660799f, -0.3980445648f),
        new(-0.8150056635f, -0.5794529907f), new(-0.8789331304f, 0.4769450202f),
        new(0.0158605829f, 0.999874213f), new(-0.8095464474f, 0.5870558317f),
        new(-0.9165898907f, -0.3998286786f), new(-0.8023542565f, 0.5968480938f),
        new(-0.5176737917f, 0.8555780767f), new(-0.8154407307f, -0.5788405779f),
        new(0.4022010347f, -0.9155513791f), new(-0.9052556868f, -0.4248672045f),
        new(0.7317445619f, 0.6815789728f), new(-0.5647632201f, -0.8252529947f),
        new(-0.8403276335f, -0.5420788397f), new(-0.9314281527f, 0.363925262f),
        new(0.5238198472f, 0.8518290719f), new(0.7432803869f, -0.6689800195f),
        new(-0.985371561f, -0.1704197369f), new(0.4601468731f, 0.88784281f),
        new(0.825855404f, 0.5638819483f), new(0.6182366099f, 0.7859920446f),
        new(0.8331502863f, -0.553046653f), new(0.1500307506f, 0.9886813308f),
        new(-0.662330369f, -0.7492119075f), new(-0.668598664f, 0.743623444f),
        new(0.7025606278f, 0.7116238924f), new(-0.5419389763f, -0.8404178401f),
        new(-0.3388616456f, 0.9408362159f), new(0.8331530315f, 0.5530425174f),
        new(-0.2989720662f, -0.9542618632f), new(0.2638522993f, 0.9645630949f),
        new(0.124108739f, -0.9922686234f), new(-0.7282649308f, -0.6852956957f),
        new(0.6962500149f, 0.7177993569f), new(-0.9183535368f, 0.3957610156f),
        new(-0.6326102274f, -0.7744703352f), new(-0.9331891859f, -0.359385508f),
        new(-0.1153779357f, -0.9933216659f), new(0.9514974788f, -0.3076565421f),
        new(-0.08987977445f, -0.9959526224f), new(0.6678496916f, 0.7442961705f),
        new(0.7952400393f, -0.6062947138f), new(-0.6462007402f, -0.7631674805f),
        new(-0.2733598753f, 0.9619118351f), new(0.9669590226f, -0.254931851f),
        new(-0.9792894595f, 0.2024651934f), new(-0.5369502995f, -0.8436138784f),
        new(-0.270036471f, -0.9628500944f), new(-0.6400277131f, 0.7683518247f),
        new(-0.7854537493f, -0.6189203566f), new(0.06005905383f, -0.9981948257f),
        new(-0.02455770378f, 0.9996984141f), new(-0.65983623f, 0.751409442f),
        new(-0.6253894466f, -0.7803127835f), new(-0.6210408851f, -0.7837781695f),
        new(0.8348888491f, 0.5504185768f), new(-0.1592275245f, 0.9872419133f),
        new(0.8367622488f, 0.5475663786f), new(-0.8675753916f, -0.4973056806f),
        new(-0.2022662628f, -0.9793305667f), new(0.9399189937f, 0.3413975472f),
        new(0.9877404807f, -0.1561049093f), new(-0.9034455656f, 0.4287028224f),
        new(0.1269804218f, -0.9919052235f), new(-0.3819600854f, 0.924178821f),
        new(0.9754625894f, 0.2201652486f), new(-0.3204015856f, -0.9472818081f),
        new(-0.9874760884f, 0.1577687387f), new(0.02535348474f, -0.9996785487f),
        new(0.4835130794f, -0.8753371362f), new(-0.2850799925f, -0.9585037287f),
        new(-0.06805516006f, -0.99768156f), new(-0.7885244045f, -0.6150034663f),
        new(0.3185392127f, -0.9479096845f), new(0.8880043089f, 0.4598351306f),
        new(0.6476921488f, -0.7619021462f), new(0.9820241299f, 0.1887554194f),
        new(0.9357275128f, -0.3527237187f), new(-0.8894895414f, 0.4569555293f),
        new(0.7922791302f, 0.6101588153f), new(0.7483818261f, 0.6632681526f),
        new(-0.7288929755f, -0.6846276581f), new(0.8729032783f, -0.4878932944f),
        new(0.8288345784f, 0.5594937369f), new(0.08074567077f, 0.9967347374f),
        new(0.9799148216f, -0.1994165048f), new(-0.580730673f, -0.8140957471f),
        new(-0.4700049791f, -0.8826637636f), new(0.2409492979f, 0.9705377045f),
        new(0.9437816757f, -0.3305694308f), new(-0.8927998638f, -0.4504535528f),
        new(-0.8069622304f, 0.5906030467f), new(0.06258973166f, 0.9980393407f),
        new(-0.9312597469f, 0.3643559849f), new(0.5777449785f, 0.8162173362f),
        new(-0.3360095855f, -0.941858566f), new(0.697932075f, -0.7161639607f),
        new(-0.002008157227f, -0.9999979837f), new(-0.1827294312f, -0.9831632392f),
        new(-0.6523911722f, 0.7578824173f), new(-0.4302626911f, -0.9027037258f),
        new(-0.9985126289f, -0.05452091251f), new(-0.01028102172f, -0.9999471489f),
        new(-0.4946071129f, 0.8691166802f), new(-0.2999350194f, 0.9539596344f),
        new(0.8165471961f, 0.5772786819f), new(0.2697460475f, 0.962931498f),
        new(-0.7306287391f, -0.6827749597f), new(-0.7590952064f, -0.6509796216f),
        new(-0.907053853f, 0.4210146171f), new(-0.5104861064f, -0.8598860013f),
        new(0.8613350597f, 0.5080373165f), new(0.5007881595f, -0.8655698812f),
        new(-0.654158152f, 0.7563577938f), new(-0.8382755311f, -0.545246856f),
        new(0.6940070834f, 0.7199681717f), new(0.06950936031f, 0.9975812994f),
        new(0.1702942185f, -0.9853932612f), new(0.2695973274f, 0.9629731466f),
        new(0.5519612192f, -0.8338697815f), new(0.225657487f, -0.9742067022f),
        new(0.4215262855f, -0.9068161835f), new(0.4881873305f, -0.8727388672f),
        new(-0.3683854996f, -0.9296731273f), new(-0.9825390578f, 0.1860564427f),
        new(0.81256471f, 0.5828709909f), new(0.3196460933f, -0.9475370046f),
        new(0.9570913859f, 0.2897862643f), new(-0.6876655497f, -0.7260276109f),
        new(-0.9988770922f, -0.047376731f), new(-0.1250179027f, 0.992154486f),
        new(-0.8280133617f, 0.560708367f), new(0.9324863769f, -0.3612051451f),
        new(0.6394653183f, 0.7688199442f), new(-0.01623847064f, -0.9998681473f),
        new(-0.9955014666f, -0.09474613458f), new(-0.81453315f, 0.580117012f),
        new(0.4037327978f, -0.9148769469f), new(0.9944263371f, 0.1054336766f),
        new(-0.1624711654f, 0.9867132919f), new(-0.9949487814f, -0.100383875f),
        new(-0.6995302564f, 0.7146029809f), new(0.5263414922f, -0.85027327f),
        new(-0.5395221479f, 0.841971408f), new(0.6579370318f, 0.7530729462f),
        new(0.01426758847f, -0.9998982128f), new(-0.6734383991f, 0.7392433447f),
        new(0.639412098f, -0.7688642071f), new(0.9211571421f, 0.3891908523f),
        new(-0.146637214f, -0.9891903394f), new(-0.782318098f, 0.6228791163f),
        new(-0.5039610839f, -0.8637263605f), new(-0.7743120191f, -0.6328039957f),
    };

    private static readonly Float3[] CELL_3D =
    {
        new(-0.7292736885f, -0.6618439697f, 0.1735581948f),
        new(0.790292081f, -0.5480887466f, -0.2739291014f),
        new(0.7217578935f, 0.6226212466f, -0.3023380997f),
        new(0.565683137f, -0.8208298145f, -0.0790000257f),
        new(0.760049034f, -0.5555979497f, -0.3370999617f),
        new(0.3713945616f, 0.5011264475f, 0.7816254623f),
        new(-0.1277062463f, -0.4254438999f, -0.8959289049f),
        new(-0.2881560924f, -0.5815838982f, 0.7607405838f),
        new(0.5849561111f, -0.662820239f, -0.4674352136f),
        new(0.3307171178f, 0.0391653737f, 0.94291689f),
        new(0.8712121778f, -0.4113374369f, -0.2679381538f),
        new(0.580981015f, 0.7021915846f, 0.4115677815f),
        new(0.503756873f, 0.6330056931f, -0.5878203852f),
        new(0.4493712205f, 0.601390195f, 0.6606022552f),
        new(-0.6878403724f, 0.09018890807f, -0.7202371714f),
        new(-0.5958956522f, -0.6469350577f, 0.475797649f),
        new(-0.5127052122f, 0.1946921978f, -0.8361987284f),
        new(-0.9911507142f, -0.05410276466f, -0.1212153153f),
        new(-0.2149721042f, 0.9720882117f, -0.09397607749f),
        new(-0.7518650936f, -0.5428057603f, 0.3742469607f),
        new(0.5237068895f, 0.8516377189f, -0.02107817834f),
        new(0.6333504779f, 0.1926167129f, -0.7495104896f),
        new(-0.06788241606f, 0.3998305789f, 0.9140719259f),
        new(-0.5538628599f, -0.4729896695f, -0.6852128902f),
        new(-0.7261455366f, -0.5911990757f, 0.3509933228f),
        new(-0.9229274737f, -0.1782808786f, 0.3412049336f),
        new(-0.6968815002f, 0.6511274338f, 0.3006480328f),
        new(0.9608044783f, -0.2098363234f, -0.1811724921f),
        new(0.06817146062f, -0.9743405129f, 0.2145069156f),
        new(-0.3577285196f, -0.6697087264f, -0.6507845481f),
        new(-0.1868621131f, 0.7648617052f, -0.6164974636f),
        new(-0.6541697588f, 0.3967914832f, 0.6439087246f),
        new(0.6993340405f, -0.6164538506f, 0.3618239211f),
        new(-0.1546665739f, 0.6291283928f, 0.7617583057f),
        new(-0.6841612949f, -0.2580482182f, -0.6821542638f),
        new(0.5383980957f, 0.4258654885f, 0.7271630328f),
        new(-0.5026987823f, -0.7939832935f, -0.3418836993f),
        new(0.3202971715f, 0.2834415347f, 0.9039195862f),
        new(0.8683227101f, -0.0003762656404f, -0.4959995258f),
        new(0.791120031f, -0.08511045745f, 0.6057105799f),
        new(-0.04011016052f, -0.4397248749f, 0.8972364289f),
        new(0.9145119872f, 0.3579346169f, -0.1885487608f),
        new(-0.9612039066f, -0.2756484276f, 0.01024666929f),
        new(0.6510361721f, -0.2877799159f, -0.7023778346f),
        new(-0.2041786351f, 0.7365237271f, 0.644859585f),
        new(-0.7718263711f, 0.3790626912f, 0.5104855816f),
        new(-0.3060082741f, -0.7692987727f, 0.5608371729f),
        new(0.454007341f, -0.5024843065f, 0.7357899537f),
        new(0.4816795475f, 0.6021208291f, -0.6367380315f),
        new(0.6961980369f, -0.3222197429f, 0.641469197f),
        new(-0.6532160499f, -0.6781148932f, 0.3368515753f),
        new(0.5089301236f, -0.6154662304f, -0.6018234363f),
        new(-0.1635919754f, -0.9133604627f, -0.372840892f),
        new(0.52408019f, -0.8437664109f, 0.1157505864f),
        new(0.5902587356f, 0.4983817807f, -0.6349883666f),
        new(0.5863227872f, 0.494764745f, 0.6414307729f),
        new(0.6779335087f, 0.2341345225f, 0.6968408593f),
        new(0.7177054546f, -0.6858979348f, 0.120178631f),
        new(-0.5328819713f, -0.5205125012f, 0.6671608058f),
        new(-0.8654874251f, -0.0700727088f, -0.4960053754f),
        new(-0.2861810166f, 0.7952089234f, 0.5345495242f),
        new(-0.04849529634f, 0.9810836427f, -0.1874115585f),
        new(-0.6358521667f, 0.6058348682f, 0.4781800233f),
        new(0.6254794696f, -0.2861619734f, 0.7258696564f),
        new(-0.2585259868f, 0.5061949264f, -0.8227581726f),
        new(0.02136306781f, 0.5064016808f, -0.8620330371f),
        new(0.200111773f, 0.8599263484f, 0.4695550591f),
        new(0.4743561372f, 0.6014985084f, -0.6427953014f),
        new(0.6622993731f, -0.5202474575f, -0.5391679918f),
        new(0.08084972818f, -0.6532720452f, 0.7527940996f),
        new(-0.6893687501f, 0.0592860349f, 0.7219805347f),
        new(-0.1121887082f, -0.9673185067f, 0.2273952515f),
        new(0.7344116094f, 0.5979668656f, -0.3210532909f),
        new(0.5789393465f, -0.2488849713f, 0.7764570201f),
        new(0.6988182827f, 0.3557169806f, -0.6205791146f),
        new(-0.8636845529f, -0.2748771249f, -0.4224826141f),
        new(-0.4247027957f, -0.4640880967f, 0.777335046f),
        new(0.5257722489f, -0.8427017621f, 0.1158329937f),
        new(0.9343830603f, 0.316302472f, -0.1639543925f),
        new(-0.1016836419f, -0.8057303073f, -0.5834887393f),
        new(-0.6529238969f, 0.50602126f, -0.5635892736f),
        new(-0.2465286165f, -0.9668205684f, -0.06694497494f),
        new(-0.9776897119f, -0.2099250524f, -0.007368825344f),
        new(0.7736893337f, 0.5734244712f, 0.2694238123f),
        new(-0.6095087895f, 0.4995678998f, 0.6155736747f),
        new(0.5794535482f, 0.7434546771f, 0.3339292269f),
        new(-0.8226211154f, 0.08142581855f, 0.5627293636f),
        new(-0.510385483f, 0.4703667658f, 0.7199039967f),
        new(-0.5764971849f, -0.07231656274f, -0.8138926898f),
        new(0.7250628871f, 0.3949971505f, -0.5641463116f),
        new(-0.1525424005f, 0.4860840828f, -0.8604958341f),
        new(-0.5550976208f, -0.4957820792f, 0.667882296f),
        new(-0.1883614327f, 0.9145869398f, 0.357841725f),
        new(0.7625556724f, -0.5414408243f, -0.3540489801f),
        new(-0.5870231946f, -0.3226498013f, -0.7424963803f),
        new(0.3051124198f, 0.2262544068f, -0.9250488391f),
        new(0.6379576059f, 0.577242424f, -0.5097070502f),
        new(-0.5966775796f, 0.1454852398f, -0.7891830656f),
        new(-0.658330573f, 0.6555487542f, -0.3699414651f),
        new(0.7434892426f, 0.2351084581f, 0.6260573129f),
        new(0.5562114096f, 0.8264360377f, -0.0873632843f),
        new(-0.3028940016f, -0.8251527185f, 0.4768419182f),
        new(0.1129343818f, -0.985888439f, -0.1235710781f),
        new(0.5937652891f, -0.5896813806f, 0.5474656618f),
        new(0.6757964092f, -0.5835758614f, -0.4502648413f),
        new(0.7242302609f, -0.1152719764f, 0.6798550586f),
        new(-0.9511914166f, 0.0753623979f, -0.2992580792f),
        new(0.2539470961f, -0.1886339355f, 0.9486454084f),
        new(0.571433621f, -0.1679450851f, -0.8032795685f),
        new(-0.06778234979f, 0.3978269256f, 0.9149531629f),
        new(0.6074972649f, 0.733060024f, -0.3058922593f),
        new(-0.5435478392f, 0.1675822484f, 0.8224791405f),
        new(-0.5876678086f, -0.3380045064f, -0.7351186982f),
        new(-0.7967562402f, 0.04097822706f, -0.6029098428f),
        new(-0.1996350917f, 0.8706294745f, 0.4496111079f),
        new(-0.02787660336f, -0.9106232682f, -0.4122962022f),
        new(-0.7797625996f, -0.6257634692f, 0.01975775581f),
        new(-0.5211232846f, 0.7401644346f, -0.4249554471f),
        new(0.8575424857f, 0.4053272873f, -0.3167501783f),
        new(0.1045223322f, 0.8390195772f, -0.5339674439f),
        new(0.3501822831f, 0.9242524096f, -0.1520850155f),
        new(0.1987849858f, 0.07647613266f, 0.9770547224f),
        new(0.7845996363f, 0.6066256811f, -0.1280964233f),
        new(0.09006737436f, -0.9750989929f, -0.2026569073f),
        new(-0.8274343547f, -0.542299559f, 0.1458203587f),
        new(-0.3485797732f, -0.415802277f, 0.840000362f),
        new(-0.2471778936f, -0.7304819962f, -0.6366310879f),
        new(-0.3700154943f, 0.8577948156f, 0.3567584454f),
        new(0.5913394901f, -0.548311967f, -0.5913303597f),
        new(0.1204873514f, -0.7626472379f, -0.6354935001f),
        new(0.616959265f, 0.03079647928f, 0.7863922953f),
        new(0.1258156836f, -0.6640829889f, -0.7369967419f),
        new(-0.6477565124f, -0.1740147258f, -0.7417077429f),
        new(0.6217889313f, -0.7804430448f, -0.06547655076f),
        new(0.6589943422f, -0.6096987708f, 0.4404473475f),
        new(-0.2689837504f, -0.6732403169f, -0.6887635427f),
        new(-0.3849775103f, 0.5676542638f, 0.7277093879f),
        new(0.5754444408f, 0.8110471154f, -0.1051963504f),
        new(0.9141593684f, 0.3832947817f, 0.131900567f),
        new(-0.107925319f, 0.9245493968f, 0.3654593525f),
        new(0.377977089f, 0.3043148782f, 0.8743716458f),
        new(-0.2142885215f, -0.8259286236f, 0.5214617324f),
        new(0.5802544474f, 0.4148098596f, -0.7008834116f),
        new(-0.1982660881f, 0.8567161266f, -0.4761596756f),
        new(-0.03381553704f, 0.3773180787f, -0.9254661404f),
        new(-0.6867922841f, -0.6656597827f, 0.2919133642f),
        new(0.7731742607f, -0.2875793547f, -0.5652430251f),
        new(-0.09655941928f, 0.9193708367f, -0.3813575004f),
        new(0.2715702457f, -0.9577909544f, -0.09426605581f),
        new(0.2451015704f, -0.6917998565f, -0.6792188003f),
        new(0.977700782f, -0.1753855374f, 0.1155036542f),
        new(-0.5224739938f, 0.8521606816f, 0.02903615945f),
        new(-0.7734880599f, -0.5261292347f, 0.3534179531f),
        new(-0.7134492443f, -0.269547243f, 0.6467878011f),
        new(0.1644037271f, 0.5105846203f, -0.8439637196f),
        new(0.6494635788f, 0.05585611296f, 0.7583384168f),
        new(-0.4711970882f, 0.5017280509f, -0.7254255765f),
        new(-0.6335764307f, -0.2381686273f, -0.7361091029f),
        new(-0.9021533097f, -0.270947803f, -0.3357181763f),
        new(-0.3793711033f, 0.872258117f, 0.3086152025f),
        new(-0.6855598966f, -0.3250143309f, 0.6514394162f),
        new(0.2900942212f, -0.7799057743f, -0.5546100667f),
        new(-0.2098319339f, 0.85037073f, 0.4825351604f),
        new(-0.4592603758f, 0.6598504336f, -0.5947077538f),
        new(0.8715945488f, 0.09616365406f, -0.4807031248f),
        new(-0.6776666319f, 0.7118504878f, -0.1844907016f),
        new(0.7044377633f, 0.312427597f, 0.637304036f),
        new(-0.7052318886f, -0.2401093292f, -0.6670798253f),
        new(0.081921007f, -0.7207336136f, -0.6883545647f),
        new(-0.6993680906f, -0.5875763221f, -0.4069869034f),
        new(-0.1281454481f, 0.6419895885f, 0.7559286424f),
        new(-0.6337388239f, -0.6785471501f, -0.3714146849f),
        new(0.5565051903f, -0.2168887573f, -0.8020356851f),
        new(-0.5791554484f, 0.7244372011f, -0.3738578718f),
        new(0.1175779076f, -0.7096451073f, 0.6946792478f),
        new(-0.6134619607f, 0.1323631078f, 0.7785527795f),
        new(0.6984635305f, -0.02980516237f, -0.715024719f),
        new(0.8318082963f, -0.3930171956f, 0.3919597455f),
        new(0.1469576422f, 0.05541651717f, -0.9875892167f),
        new(0.708868575f, -0.2690503865f, 0.6520101478f),
        new(0.2726053183f, 0.67369766f, -0.68688995f),
        new(-0.6591295371f, 0.3035458599f, -0.6880466294f),
        new(0.4815131379f, -0.7528270071f, 0.4487723203f),
        new(0.9430009463f, 0.1675647412f, -0.2875261255f),
        new(0.434802957f, 0.7695304522f, -0.4677277752f),
        new(0.3931996188f, 0.594473625f, 0.7014236729f),
        new(0.7254336655f, -0.603925654f, 0.3301814672f),
        new(0.7590235227f, -0.6506083235f, 0.02433313207f),
        new(-0.8552768592f, -0.3430042733f, 0.3883935666f),
        new(-0.6139746835f, 0.6981725247f, 0.3682257648f),
        new(-0.7465905486f, -0.5752009504f, 0.3342849376f),
        new(0.5730065677f, 0.810555537f, -0.1210916791f),
        new(-0.9225877367f, -0.3475211012f, -0.167514036f),
        new(-0.7105816789f, -0.4719692027f, -0.5218416899f),
        new(-0.08564609717f, 0.3583001386f, 0.929669703f),
        new(-0.8279697606f, -0.2043157126f, 0.5222271202f),
        new(0.427944023f, 0.278165994f, 0.8599346446f),
        new(0.5399079671f, -0.7857120652f, -0.3019204161f),
        new(0.5678404253f, -0.5495413974f, -0.6128307303f),
        new(-0.9896071041f, 0.1365639107f, -0.04503418428f),
        new(-0.6154342638f, -0.6440875597f, 0.4543037336f),
        new(0.1074204368f, -0.7946340692f, 0.5975094525f),
        new(-0.3595449969f, -0.8885529948f, 0.28495784f),
        new(-0.2180405296f, 0.1529888965f, 0.9638738118f),
        new(-0.7277432317f, -0.6164050508f, -0.3007234646f),
        new(0.7249729114f, -0.00669719484f, 0.6887448187f),
        new(-0.5553659455f, -0.5336586252f, 0.6377908264f),
        new(0.5137558015f, 0.7976208196f, -0.3160000073f),
        new(-0.3794024848f, 0.9245608561f, -0.03522751494f),
        new(0.8229248658f, 0.2745365933f, -0.4974176556f),
        new(-0.5404114394f, 0.6091141441f, 0.5804613989f),
        new(0.8036581901f, -0.2703029469f, 0.5301601931f),
        new(0.6044318879f, 0.6832968393f, 0.4095943388f),
        new(0.06389988817f, 0.9658208605f, -0.2512108074f),
        new(0.1087113286f, 0.7402471173f, -0.6634877936f),
        new(-0.713427712f, -0.6926784018f, 0.1059128479f),
        new(0.6458897819f, -0.5724548511f, -0.5050958653f),
        new(-0.6553931414f, 0.7381471625f, 0.159995615f),
        new(0.3910961323f, 0.9188871375f, -0.05186755998f),
        new(-0.4879022471f, -0.5904376907f, 0.6429111375f),
        new(0.6014790094f, 0.7707441366f, -0.2101820095f),
        new(-0.5677173047f, 0.7511360995f, 0.3368851762f),
        new(0.7858573506f, 0.226674665f, 0.5753666838f),
        new(-0.4520345543f, -0.604222686f, -0.6561857263f),
        new(0.002272116345f, 0.4132844051f, -0.9105991643f),
        new(-0.5815751419f, -0.5162925989f, 0.6286591339f),
        new(-0.03703704785f, 0.8273785755f, 0.5604221175f),
        new(-0.5119692504f, 0.7953543429f, -0.3244980058f),
        new(-0.2682417366f, -0.9572290247f, -0.1084387619f),
        new(-0.2322482736f, -0.9679131102f, -0.09594243324f),
        new(0.3554328906f, -0.8881505545f, 0.2913006227f),
        new(0.7346520519f, -0.4371373164f, 0.5188422971f),
        new(0.9985120116f, 0.04659011161f, -0.02833944577f),
        new(-0.3727687496f, -0.9082481361f, 0.1900757285f),
        new(0.91737377f, -0.3483642108f, 0.1925298489f),
        new(0.2714911074f, 0.4147529736f, -0.8684886582f),
        new(0.5131763485f, -0.7116334161f, 0.4798207128f),
        new(-0.8737353606f, 0.18886992f, -0.4482350644f),
        new(0.8460043821f, -0.3725217914f, 0.3814499973f),
        new(0.8978727456f, -0.1780209141f, -0.4026575304f),
        new(0.2178065647f, -0.9698322841f, -0.1094789531f),
        new(-0.1518031304f, -0.7788918132f, -0.6085091231f),
        new(-0.2600384876f, -0.4755398075f, -0.8403819825f),
        new(0.572313509f, -0.7474340931f, -0.3373418503f),
        new(-0.7174141009f, 0.1699017182f, -0.6756111411f),
        new(-0.684180784f, 0.02145707593f, -0.7289967412f),
        new(-0.2007447902f, 0.06555605789f, -0.9774476623f),
        new(-0.1148803697f, -0.8044887315f, 0.5827524187f),
        new(-0.7870349638f, 0.03447489231f, 0.6159443543f),
        new(-0.2015596421f, 0.6859872284f, 0.6991389226f),
        new(-0.08581082512f, -0.10920836f, -0.9903080513f),
        new(0.5532693395f, 0.7325250401f, -0.396610771f),
        new(-0.1842489331f, -0.9777375055f, -0.1004076743f),
        new(0.0775473789f, -0.9111505856f, 0.4047110257f),
        new(0.1399838409f, 0.7601631212f, -0.6344734459f),
        new(0.4484419361f, -0.845289248f, 0.2904925424f),
    };

    [MethodImpl(FN_INLINE)]
    private static int FastFloor(FN_DECIMAL f)
    {
        return (f >= 0 ? (int) f : (int) f - 1);
    }

    [MethodImpl(FN_INLINE)]
    private static int FastRound(FN_DECIMAL f)
    {
        return (f >= 0) ? (int) (f + (FN_DECIMAL) 0.5) : (int) (f - (FN_DECIMAL) 0.5);
    }

    [MethodImpl(FN_INLINE)]
    private static FN_DECIMAL Lerp(FN_DECIMAL a, FN_DECIMAL b, FN_DECIMAL t)
    {
        return a + t * (b - a);
    }

    [MethodImpl(FN_INLINE)]
    private static FN_DECIMAL InterpHermiteFunc(FN_DECIMAL t)
    {
        return t * t * (3 - 2 * t);
    }

    [MethodImpl(FN_INLINE)]
    private static FN_DECIMAL InterpQuinticFunc(FN_DECIMAL t)
    {
        return t * t * t * (t * (t * 6 - 15) + 10);
    }

    [MethodImpl(FN_INLINE)]
    private static FN_DECIMAL CubicLerp(FN_DECIMAL a, FN_DECIMAL b, FN_DECIMAL c, FN_DECIMAL d, FN_DECIMAL t)
    {
        FN_DECIMAL p = (d - c) - (a - b);
        return t * t * t * p + t * t * ((a - b) - p) + t * (c - a) + b;
    }

    private void CalculateFractalBounding()
    {
        FN_DECIMAL amp = m_gain;
        FN_DECIMAL ampFractal = 1;
        for (int i = 1; i < m_octaves; i++)
        {
            ampFractal += amp;
            amp *= m_gain;
        }

        m_fractalBounding = 1 / ampFractal;
    }

    // Hashing
    private const int X_PRIME = 1619;
    private const int Y_PRIME = 31337;
    private const int Z_PRIME = 6971;
    private const int W_PRIME = 1013;

    [MethodImpl(FN_INLINE)]
    private static int Hash2D(int seed, int x, int y)
    {
        int hash = seed;
        hash ^= X_PRIME * x;
        hash ^= Y_PRIME * y;

        hash = hash * hash * hash * 60493;
        hash = (hash >> 13) ^ hash;

        return hash;
    }

    [MethodImpl(FN_INLINE)]
    private static int Hash3D(int seed, int x, int y, int z)
    {
        int hash = seed;
        hash ^= X_PRIME * x;
        hash ^= Y_PRIME * y;
        hash ^= Z_PRIME * z;

        hash = hash * hash * hash * 60493;
        hash = (hash >> 13) ^ hash;

        return hash;
    }

    [MethodImpl(FN_INLINE)]
    private static int Hash4D(int seed, int x, int y, int z, int w)
    {
        int hash = seed;
        hash ^= X_PRIME * x;
        hash ^= Y_PRIME * y;
        hash ^= Z_PRIME * z;
        hash ^= W_PRIME * w;

        hash = hash * hash * hash * 60493;
        hash = (hash >> 13) ^ hash;

        return hash;
    }

    [MethodImpl(FN_INLINE)]
    private static FN_DECIMAL ValCoord2D(int seed, int x, int y)
    {
        int n = seed;
        n ^= X_PRIME * x;
        n ^= Y_PRIME * y;

        return (n * n * n * 60493) / (FN_DECIMAL) 2147483648.0;
    }

    [MethodImpl(FN_INLINE)]
    private static FN_DECIMAL ValCoord3D(int seed, int x, int y, int z)
    {
        int n = seed;
        n ^= X_PRIME * x;
        n ^= Y_PRIME * y;
        n ^= Z_PRIME * z;

        return (n * n * n * 60493) / (FN_DECIMAL) 2147483648.0;
    }

    [MethodImpl(FN_INLINE)]
    private static FN_DECIMAL ValCoord4D(int seed, int x, int y, int z, int w)
    {
        int n = seed;
        n ^= X_PRIME * x;
        n ^= Y_PRIME * y;
        n ^= Z_PRIME * z;
        n ^= W_PRIME * w;

        return (n * n * n * 60493) / (FN_DECIMAL) 2147483648.0;
    }

    [MethodImpl(FN_INLINE)]
    private static FN_DECIMAL GradCoord2D(int seed, int x, int y, FN_DECIMAL xd, FN_DECIMAL yd)
    {
        int hash = seed;
        hash ^= X_PRIME * x;
        hash ^= Y_PRIME * y;

        hash = hash * hash * hash * 60493;
        hash = (hash >> 13) ^ hash;

        Float2 g = GRAD_2D[hash & 7];

        return xd * g.x + yd * g.y;
    }

    [MethodImpl(FN_INLINE)]
    private static FN_DECIMAL GradCoord3D(int seed, int x, int y, int z, FN_DECIMAL xd, FN_DECIMAL yd,
        FN_DECIMAL zd)
    {
        int hash = seed;
        hash ^= X_PRIME * x;
        hash ^= Y_PRIME * y;
        hash ^= Z_PRIME * z;

        hash = hash * hash * hash * 60493;
        hash = (hash >> 13) ^ hash;

        Float3 g = GRAD_3D[hash & 15];

        return xd * g.x + yd * g.y + zd * g.z;
    }

    [MethodImpl(FN_INLINE)]
    private static FN_DECIMAL GradCoord4D(int seed, int x, int y, int z, int w, FN_DECIMAL xd, FN_DECIMAL yd,
        FN_DECIMAL zd, FN_DECIMAL wd)
    {
        int hash = seed;
        hash ^= X_PRIME * x;
        hash ^= Y_PRIME * y;
        hash ^= Z_PRIME * z;
        hash ^= W_PRIME * w;

        hash = hash * hash * hash * 60493;
        hash = (hash >> 13) ^ hash;

        hash &= 31;
        FN_DECIMAL a = yd, b = zd, c = wd; // X,Y,Z
        switch (hash >> 3)
        {
            // OR, DEPENDING ON HIGH ORDER 2 BITS:
            case 1:
                a = wd;
                b = xd;
                c = yd;
                break; // W,X,Y
            case 2:
                a = zd;
                b = wd;
                c = xd;
                break; // Z,W,X
            case 3:
                a = yd;
                b = zd;
                c = wd;
                break; // Y,Z,W
        }

        return ((hash & 4) == 0 ? -a : a) + ((hash & 2) == 0 ? -b : b) + ((hash & 1) == 0 ? -c : c);
    }

    public FN_DECIMAL GetNoise(FN_DECIMAL x, FN_DECIMAL y, FN_DECIMAL z)
    {
        x *= m_frequency;
        y *= m_frequency;
        z *= m_frequency;

        switch (m_noiseType)
        {
            case NoiseType.Value:
                return SingleValue(m_seed, x, y, z);
            case NoiseType.ValueFractal:
                switch (m_fractalType)
                {
                    case FractalType.FBM:
                        return SingleValueFractalFBM(x, y, z);
                    case FractalType.Billow:
                        return SingleValueFractalBillow(x, y, z);
                    case FractalType.RigidMulti:
                        return SingleValueFractalRigidMulti(x, y, z);
                    default:
                        return 0;
                }
            case NoiseType.Perlin:
                return SinglePerlin(m_seed, x, y, z);
            case NoiseType.PerlinFractal:
                switch (m_fractalType)
                {
                    case FractalType.FBM:
                        return SinglePerlinFractalFBM(x, y, z);
                    case FractalType.Billow:
                        return SinglePerlinFractalBillow(x, y, z);
                    case FractalType.RigidMulti:
                        return SinglePerlinFractalRigidMulti(x, y, z);
                    default:
                        return 0;
                }
            case NoiseType.Simplex:
                return SingleSimplex(m_seed, x, y, z);
            case NoiseType.SimplexFractal:
                switch (m_fractalType)
                {
                    case FractalType.FBM:
                        return SingleSimplexFractalFBM(x, y, z);
                    case FractalType.Billow:
                        return SingleSimplexFractalBillow(x, y, z);
                    case FractalType.RigidMulti:
                        return SingleSimplexFractalRigidMulti(x, y, z);
                    default:
                        return 0;
                }
            case NoiseType.Cellular:
                switch (m_cellularReturnType)
                {
                    case CellularReturnType.CellValue:
                    case CellularReturnType.NoiseLookup:
                    case CellularReturnType.Distance:
                        return SingleCellular(x, y, z);
                    default:
                        return SingleCellular2Edge(x, y, z);
                }
            case NoiseType.WhiteNoise:
                return GetWhiteNoise(x, y, z);
            case NoiseType.Cubic:
                return SingleCubic(m_seed, x, y, z);
            case NoiseType.CubicFractal:
                switch (m_fractalType)
                {
                    case FractalType.FBM:
                        return SingleCubicFractalFBM(x, y, z);
                    case FractalType.Billow:
                        return SingleCubicFractalBillow(x, y, z);
                    case FractalType.RigidMulti:
                        return SingleCubicFractalRigidMulti(x, y, z);
                    default:
                        return 0;
                }
            default:
                return 0;
        }
    }

    public FN_DECIMAL GetNoise(FN_DECIMAL x, FN_DECIMAL y)
    {
        x *= m_frequency;
        y *= m_frequency;

        switch (m_noiseType)
        {
            case NoiseType.Value:
                return SingleValue(m_seed, x, y);
            case NoiseType.ValueFractal:
                switch (m_fractalType)
                {
                    case FractalType.FBM:
                        return SingleValueFractalFBM(x, y);
                    case FractalType.Billow:
                        return SingleValueFractalBillow(x, y);
                    case FractalType.RigidMulti:
                        return SingleValueFractalRigidMulti(x, y);
                    default:
                        return 0;
                }
            case NoiseType.Perlin:
                return SinglePerlin(m_seed, x, y);
            case NoiseType.PerlinFractal:
                switch (m_fractalType)
                {
                    case FractalType.FBM:
                        return SinglePerlinFractalFBM(x, y);
                    case FractalType.Billow:
                        return SinglePerlinFractalBillow(x, y);
                    case FractalType.RigidMulti:
                        return SinglePerlinFractalRigidMulti(x, y);
                    default:
                        return 0;
                }
            case NoiseType.Simplex:
                return SingleSimplex(m_seed, x, y);
            case NoiseType.SimplexFractal:
                switch (m_fractalType)
                {
                    case FractalType.FBM:
                        return SingleSimplexFractalFBM(x, y);
                    case FractalType.Billow:
                        return SingleSimplexFractalBillow(x, y);
                    case FractalType.RigidMulti:
                        return SingleSimplexFractalRigidMulti(x, y);
                    default:
                        return 0;
                }
            case NoiseType.Cellular:
                switch (m_cellularReturnType)
                {
                    case CellularReturnType.CellValue:
                    case CellularReturnType.NoiseLookup:
                    case CellularReturnType.Distance:
                        return SingleCellular(x, y);
                    default:
                        return SingleCellular2Edge(x, y);
                }
            case NoiseType.WhiteNoise:
                return GetWhiteNoise(x, y);
            case NoiseType.Cubic:
                return SingleCubic(m_seed, x, y);
            case NoiseType.CubicFractal:
                switch (m_fractalType)
                {
                    case FractalType.FBM:
                        return SingleCubicFractalFBM(x, y);
                    case FractalType.Billow:
                        return SingleCubicFractalBillow(x, y);
                    case FractalType.RigidMulti:
                        return SingleCubicFractalRigidMulti(x, y);
                    default:
                        return 0;
                }
            default:
                return 0;
        }
    }

    // White Noise
    [MethodImpl(FN_INLINE)]
    private int FloatCast2Int(FN_DECIMAL f)
    {
        var i = BitConverter.DoubleToInt64Bits(f);

        return (int) (i ^ (i >> 32));
    }

    public FN_DECIMAL GetWhiteNoise(FN_DECIMAL x, FN_DECIMAL y, FN_DECIMAL z, FN_DECIMAL w)
    {
        int xi = FloatCast2Int(x);
        int yi = FloatCast2Int(y);
        int zi = FloatCast2Int(z);
        int wi = FloatCast2Int(w);

        return ValCoord4D(m_seed, xi, yi, zi, wi);
    }

    public FN_DECIMAL GetWhiteNoise(FN_DECIMAL x, FN_DECIMAL y, FN_DECIMAL z)
    {
        int xi = FloatCast2Int(x);
        int yi = FloatCast2Int(y);
        int zi = FloatCast2Int(z);

        return ValCoord3D(m_seed, xi, yi, zi);
    }

    public FN_DECIMAL GetWhiteNoise(FN_DECIMAL x, FN_DECIMAL y)
    {
        int xi = FloatCast2Int(x);
        int yi = FloatCast2Int(y);

        return ValCoord2D(m_seed, xi, yi);
    }

    public FN_DECIMAL GetWhiteNoiseInt(int x, int y, int z, int w)
    {
        return ValCoord4D(m_seed, x, y, z, w);
    }

    public FN_DECIMAL GetWhiteNoiseInt(int x, int y, int z)
    {
        return ValCoord3D(m_seed, x, y, z);
    }

    public FN_DECIMAL GetWhiteNoiseInt(int x, int y)
    {
        return ValCoord2D(m_seed, x, y);
    }

    // Value Noise
    public FN_DECIMAL GetValueFractal(FN_DECIMAL x, FN_DECIMAL y, FN_DECIMAL z)
    {
        x *= m_frequency;
        y *= m_frequency;
        z *= m_frequency;

        switch (m_fractalType)
        {
            case FractalType.FBM:
                return SingleValueFractalFBM(x, y, z);
            case FractalType.Billow:
                return SingleValueFractalBillow(x, y, z);
            case FractalType.RigidMulti:
                return SingleValueFractalRigidMulti(x, y, z);
            default:
                return 0;
        }
    }

    private FN_DECIMAL SingleValueFractalFBM(FN_DECIMAL x, FN_DECIMAL y, FN_DECIMAL z)
    {
        int seed = m_seed;
        FN_DECIMAL sum = SingleValue(seed, x, y, z);
        FN_DECIMAL amp = 1;

        for (int i = 1; i < m_octaves; i++)
        {
            x *= m_lacunarity;
            y *= m_lacunarity;
            z *= m_lacunarity;

            amp *= m_gain;
            sum += SingleValue(++seed, x, y, z) * amp;
        }

        return sum * m_fractalBounding;
    }

    private FN_DECIMAL SingleValueFractalBillow(FN_DECIMAL x, FN_DECIMAL y, FN_DECIMAL z)
    {
        int seed = m_seed;
        FN_DECIMAL sum = Math.Abs(SingleValue(seed, x, y, z)) * 2 - 1;
        FN_DECIMAL amp = 1;

        for (int i = 1; i < m_octaves; i++)
        {
            x *= m_lacunarity;
            y *= m_lacunarity;
            z *= m_lacunarity;

            amp *= m_gain;
            sum += (Math.Abs(SingleValue(++seed, x, y, z)) * 2 - 1) * amp;
        }

        return sum * m_fractalBounding;
    }

    private FN_DECIMAL SingleValueFractalRigidMulti(FN_DECIMAL x, FN_DECIMAL y, FN_DECIMAL z)
    {
        int seed = m_seed;
        FN_DECIMAL sum = 1 - Math.Abs(SingleValue(seed, x, y, z));
        FN_DECIMAL amp = 1;

        for (int i = 1; i < m_octaves; i++)
        {
            x *= m_lacunarity;
            y *= m_lacunarity;
            z *= m_lacunarity;

            amp *= m_gain;
            sum -= (1 - Math.Abs(SingleValue(++seed, x, y, z))) * amp;
        }

        return sum;
    }

    public FN_DECIMAL GetValue(FN_DECIMAL x, FN_DECIMAL y, FN_DECIMAL z)
    {
        return SingleValue(m_seed, x * m_frequency, y * m_frequency, z * m_frequency);
    }

    private FN_DECIMAL SingleValue(int seed, FN_DECIMAL x, FN_DECIMAL y, FN_DECIMAL z)
    {
        int x0 = FastFloor(x);
        int y0 = FastFloor(y);
        int z0 = FastFloor(z);
        int x1 = x0 + 1;
        int y1 = y0 + 1;
        int z1 = z0 + 1;

        FN_DECIMAL xs, ys, zs;
        switch (m_interp)
        {
            default:
            case Interp.Linear:
                xs = x - x0;
                ys = y - y0;
                zs = z - z0;
                break;
            case Interp.Hermite:
                xs = InterpHermiteFunc(x - x0);
                ys = InterpHermiteFunc(y - y0);
                zs = InterpHermiteFunc(z - z0);
                break;
            case Interp.Quintic:
                xs = InterpQuinticFunc(x - x0);
                ys = InterpQuinticFunc(y - y0);
                zs = InterpQuinticFunc(z - z0);
                break;
        }

        FN_DECIMAL xf00 = Lerp(ValCoord3D(seed, x0, y0, z0), ValCoord3D(seed, x1, y0, z0), xs);
        FN_DECIMAL xf10 = Lerp(ValCoord3D(seed, x0, y1, z0), ValCoord3D(seed, x1, y1, z0), xs);
        FN_DECIMAL xf01 = Lerp(ValCoord3D(seed, x0, y0, z1), ValCoord3D(seed, x1, y0, z1), xs);
        FN_DECIMAL xf11 = Lerp(ValCoord3D(seed, x0, y1, z1), ValCoord3D(seed, x1, y1, z1), xs);

        FN_DECIMAL yf0 = Lerp(xf00, xf10, ys);
        FN_DECIMAL yf1 = Lerp(xf01, xf11, ys);

        return Lerp(yf0, yf1, zs);
    }

    public FN_DECIMAL GetValueFractal(FN_DECIMAL x, FN_DECIMAL y)
    {
        x *= m_frequency;
        y *= m_frequency;

        switch (m_fractalType)
        {
            case FractalType.FBM:
                return SingleValueFractalFBM(x, y);
            case FractalType.Billow:
                return SingleValueFractalBillow(x, y);
            case FractalType.RigidMulti:
                return SingleValueFractalRigidMulti(x, y);
            default:
                return 0;
        }
    }

    private FN_DECIMAL SingleValueFractalFBM(FN_DECIMAL x, FN_DECIMAL y)
    {
        int seed = m_seed;
        FN_DECIMAL sum = SingleValue(seed, x, y);
        FN_DECIMAL amp = 1;

        for (int i = 1; i < m_octaves; i++)
        {
            x *= m_lacunarity;
            y *= m_lacunarity;

            amp *= m_gain;
            sum += SingleValue(++seed, x, y) * amp;
        }

        return sum * m_fractalBounding;
    }

    private FN_DECIMAL SingleValueFractalBillow(FN_DECIMAL x, FN_DECIMAL y)
    {
        int seed = m_seed;
        FN_DECIMAL sum = Math.Abs(SingleValue(seed, x, y)) * 2 - 1;
        FN_DECIMAL amp = 1;

        for (int i = 1; i < m_octaves; i++)
        {
            x *= m_lacunarity;
            y *= m_lacunarity;
            amp *= m_gain;
            sum += (Math.Abs(SingleValue(++seed, x, y)) * 2 - 1) * amp;
        }

        return sum * m_fractalBounding;
    }

    private FN_DECIMAL SingleValueFractalRigidMulti(FN_DECIMAL x, FN_DECIMAL y)
    {
        int seed = m_seed;
        FN_DECIMAL sum = 1 - Math.Abs(SingleValue(seed, x, y));
        FN_DECIMAL amp = 1;

        for (int i = 1; i < m_octaves; i++)
        {
            x *= m_lacunarity;
            y *= m_lacunarity;

            amp *= m_gain;
            sum -= (1 - Math.Abs(SingleValue(++seed, x, y))) * amp;
        }

        return sum;
    }

    public FN_DECIMAL GetValue(FN_DECIMAL x, FN_DECIMAL y)
    {
        return SingleValue(m_seed, x * m_frequency, y * m_frequency);
    }

    private FN_DECIMAL SingleValue(int seed, FN_DECIMAL x, FN_DECIMAL y)
    {
        int x0 = FastFloor(x);
        int y0 = FastFloor(y);
        int x1 = x0 + 1;
        int y1 = y0 + 1;

        FN_DECIMAL xs, ys;
        switch (m_interp)
        {
            default:
            case Interp.Linear:
                xs = x - x0;
                ys = y - y0;
                break;
            case Interp.Hermite:
                xs = InterpHermiteFunc(x - x0);
                ys = InterpHermiteFunc(y - y0);
                break;
            case Interp.Quintic:
                xs = InterpQuinticFunc(x - x0);
                ys = InterpQuinticFunc(y - y0);
                break;
        }

        FN_DECIMAL xf0 = Lerp(ValCoord2D(seed, x0, y0), ValCoord2D(seed, x1, y0), xs);
        FN_DECIMAL xf1 = Lerp(ValCoord2D(seed, x0, y1), ValCoord2D(seed, x1, y1), xs);

        return Lerp(xf0, xf1, ys);
    }

    // Gradient Noise
    public FN_DECIMAL GetPerlinFractal(FN_DECIMAL x, FN_DECIMAL y, FN_DECIMAL z)
    {
        x *= m_frequency;
        y *= m_frequency;
        z *= m_frequency;

        switch (m_fractalType)
        {
            case FractalType.FBM:
                return SinglePerlinFractalFBM(x, y, z);
            case FractalType.Billow:
                return SinglePerlinFractalBillow(x, y, z);
            case FractalType.RigidMulti:
                return SinglePerlinFractalRigidMulti(x, y, z);
            default:
                return 0;
        }
    }

    private FN_DECIMAL SinglePerlinFractalFBM(FN_DECIMAL x, FN_DECIMAL y, FN_DECIMAL z)
    {
        int seed = m_seed;
        FN_DECIMAL sum = SinglePerlin(seed, x, y, z);
        FN_DECIMAL amp = 1;

        for (int i = 1; i < m_octaves; i++)
        {
            x *= m_lacunarity;
            y *= m_lacunarity;
            z *= m_lacunarity;

            amp *= m_gain;
            sum += SinglePerlin(++seed, x, y, z) * amp;
        }

        return sum * m_fractalBounding;
    }

    private FN_DECIMAL SinglePerlinFractalBillow(FN_DECIMAL x, FN_DECIMAL y, FN_DECIMAL z)
    {
        int seed = m_seed;
        FN_DECIMAL sum = Math.Abs(SinglePerlin(seed, x, y, z)) * 2 - 1;
        FN_DECIMAL amp = 1;

        for (int i = 1; i < m_octaves; i++)
        {
            x *= m_lacunarity;
            y *= m_lacunarity;
            z *= m_lacunarity;

            amp *= m_gain;
            sum += (Math.Abs(SinglePerlin(++seed, x, y, z)) * 2 - 1) * amp;
        }

        return sum * m_fractalBounding;
    }

    private FN_DECIMAL SinglePerlinFractalRigidMulti(FN_DECIMAL x, FN_DECIMAL y, FN_DECIMAL z)
    {
        int seed = m_seed;
        FN_DECIMAL sum = 1 - Math.Abs(SinglePerlin(seed, x, y, z));
        FN_DECIMAL amp = 1;

        for (int i = 1; i < m_octaves; i++)
        {
            x *= m_lacunarity;
            y *= m_lacunarity;
            z *= m_lacunarity;

            amp *= m_gain;
            sum -= (1 - Math.Abs(SinglePerlin(++seed, x, y, z))) * amp;
        }

        return sum;
    }

    public FN_DECIMAL GetPerlin(FN_DECIMAL x, FN_DECIMAL y, FN_DECIMAL z)
    {
        return SinglePerlin(m_seed, x * m_frequency, y * m_frequency, z * m_frequency);
    }

    private FN_DECIMAL SinglePerlin(int seed, FN_DECIMAL x, FN_DECIMAL y, FN_DECIMAL z)
    {
        int x0 = FastFloor(x);
        int y0 = FastFloor(y);
        int z0 = FastFloor(z);
        int x1 = x0 + 1;
        int y1 = y0 + 1;
        int z1 = z0 + 1;

        FN_DECIMAL xs, ys, zs;
        switch (m_interp)
        {
            default:
            case Interp.Linear:
                xs = x - x0;
                ys = y - y0;
                zs = z - z0;
                break;
            case Interp.Hermite:
                xs = InterpHermiteFunc(x - x0);
                ys = InterpHermiteFunc(y - y0);
                zs = InterpHermiteFunc(z - z0);
                break;
            case Interp.Quintic:
                xs = InterpQuinticFunc(x - x0);
                ys = InterpQuinticFunc(y - y0);
                zs = InterpQuinticFunc(z - z0);
                break;
        }

        FN_DECIMAL xd0 = x - x0;
        FN_DECIMAL yd0 = y - y0;
        FN_DECIMAL zd0 = z - z0;
        FN_DECIMAL xd1 = xd0 - 1;
        FN_DECIMAL yd1 = yd0 - 1;
        FN_DECIMAL zd1 = zd0 - 1;

        FN_DECIMAL xf00 = Lerp(GradCoord3D(seed, x0, y0, z0, xd0, yd0, zd0),
            GradCoord3D(seed, x1, y0, z0, xd1, yd0, zd0), xs);
        FN_DECIMAL xf10 = Lerp(GradCoord3D(seed, x0, y1, z0, xd0, yd1, zd0),
            GradCoord3D(seed, x1, y1, z0, xd1, yd1, zd0), xs);
        FN_DECIMAL xf01 = Lerp(GradCoord3D(seed, x0, y0, z1, xd0, yd0, zd1),
            GradCoord3D(seed, x1, y0, z1, xd1, yd0, zd1), xs);
        FN_DECIMAL xf11 = Lerp(GradCoord3D(seed, x0, y1, z1, xd0, yd1, zd1),
            GradCoord3D(seed, x1, y1, z1, xd1, yd1, zd1), xs);

        FN_DECIMAL yf0 = Lerp(xf00, xf10, ys);
        FN_DECIMAL yf1 = Lerp(xf01, xf11, ys);

        return Lerp(yf0, yf1, zs);
    }

    public FN_DECIMAL GetPerlinFractal(FN_DECIMAL x, FN_DECIMAL y)
    {
        x *= m_frequency;
        y *= m_frequency;

        switch (m_fractalType)
        {
            case FractalType.FBM:
                return SinglePerlinFractalFBM(x, y);
            case FractalType.Billow:
                return SinglePerlinFractalBillow(x, y);
            case FractalType.RigidMulti:
                return SinglePerlinFractalRigidMulti(x, y);
            default:
                return 0;
        }
    }

    private FN_DECIMAL SinglePerlinFractalFBM(FN_DECIMAL x, FN_DECIMAL y)
    {
        int seed = m_seed;
        FN_DECIMAL sum = SinglePerlin(seed, x, y);
        FN_DECIMAL amp = 1;

        for (int i = 1; i < m_octaves; i++)
        {
            x *= m_lacunarity;
            y *= m_lacunarity;

            amp *= m_gain;
            sum += SinglePerlin(++seed, x, y) * amp;
        }

        return sum * m_fractalBounding;
    }

    private FN_DECIMAL SinglePerlinFractalBillow(FN_DECIMAL x, FN_DECIMAL y)
    {
        int seed = m_seed;
        FN_DECIMAL sum = Math.Abs(SinglePerlin(seed, x, y)) * 2 - 1;
        FN_DECIMAL amp = 1;

        for (int i = 1; i < m_octaves; i++)
        {
            x *= m_lacunarity;
            y *= m_lacunarity;

            amp *= m_gain;
            sum += (Math.Abs(SinglePerlin(++seed, x, y)) * 2 - 1) * amp;
        }

        return sum * m_fractalBounding;
    }

    private FN_DECIMAL SinglePerlinFractalRigidMulti(FN_DECIMAL x, FN_DECIMAL y)
    {
        int seed = m_seed;
        FN_DECIMAL sum = 1 - Math.Abs(SinglePerlin(seed, x, y));
        FN_DECIMAL amp = 1;

        for (int i = 1; i < m_octaves; i++)
        {
            x *= m_lacunarity;
            y *= m_lacunarity;

            amp *= m_gain;
            sum -= (1 - Math.Abs(SinglePerlin(++seed, x, y))) * amp;
        }

        return sum;
    }

    public FN_DECIMAL GetPerlin(FN_DECIMAL x, FN_DECIMAL y)
    {
        return SinglePerlin(m_seed, x * m_frequency, y * m_frequency);
    }

    private FN_DECIMAL SinglePerlin(int seed, FN_DECIMAL x, FN_DECIMAL y)
    {
        int x0 = FastFloor(x);
        int y0 = FastFloor(y);
        int x1 = x0 + 1;
        int y1 = y0 + 1;

        FN_DECIMAL xs, ys;
        switch (m_interp)
        {
            default:
            case Interp.Linear:
                xs = x - x0;
                ys = y - y0;
                break;
            case Interp.Hermite:
                xs = InterpHermiteFunc(x - x0);
                ys = InterpHermiteFunc(y - y0);
                break;
            case Interp.Quintic:
                xs = InterpQuinticFunc(x - x0);
                ys = InterpQuinticFunc(y - y0);
                break;
        }

        FN_DECIMAL xd0 = x - x0;
        FN_DECIMAL yd0 = y - y0;
        FN_DECIMAL xd1 = xd0 - 1;
        FN_DECIMAL yd1 = yd0 - 1;

        FN_DECIMAL xf0 = Lerp(GradCoord2D(seed, x0, y0, xd0, yd0), GradCoord2D(seed, x1, y0, xd1, yd0), xs);
        FN_DECIMAL xf1 = Lerp(GradCoord2D(seed, x0, y1, xd0, yd1), GradCoord2D(seed, x1, y1, xd1, yd1), xs);

        return Lerp(xf0, xf1, ys);
    }

    // Simplex Noise
    public FN_DECIMAL GetSimplexFractal(FN_DECIMAL x, FN_DECIMAL y, FN_DECIMAL z)
    {
        x *= m_frequency;
        y *= m_frequency;
        z *= m_frequency;

        switch (m_fractalType)
        {
            case FractalType.FBM:
                return SingleSimplexFractalFBM(x, y, z);
            case FractalType.Billow:
                return SingleSimplexFractalBillow(x, y, z);
            case FractalType.RigidMulti:
                return SingleSimplexFractalRigidMulti(x, y, z);
            default:
                return 0;
        }
    }

    private FN_DECIMAL SingleSimplexFractalFBM(FN_DECIMAL x, FN_DECIMAL y, FN_DECIMAL z)
    {
        int seed = m_seed;
        FN_DECIMAL sum = SingleSimplex(seed, x, y, z);
        FN_DECIMAL amp = 1;

        for (int i = 1; i < m_octaves; i++)
        {
            x *= m_lacunarity;
            y *= m_lacunarity;
            z *= m_lacunarity;

            amp *= m_gain;
            sum += SingleSimplex(++seed, x, y, z) * amp;
        }

        return sum * m_fractalBounding;
    }

    private FN_DECIMAL SingleSimplexFractalBillow(FN_DECIMAL x, FN_DECIMAL y, FN_DECIMAL z)
    {
        int seed = m_seed;
        FN_DECIMAL sum = Math.Abs(SingleSimplex(seed, x, y, z)) * 2 - 1;
        FN_DECIMAL amp = 1;

        for (int i = 1; i < m_octaves; i++)
        {
            x *= m_lacunarity;
            y *= m_lacunarity;
            z *= m_lacunarity;

            amp *= m_gain;
            sum += (Math.Abs(SingleSimplex(++seed, x, y, z)) * 2 - 1) * amp;
        }

        return sum * m_fractalBounding;
    }

    private FN_DECIMAL SingleSimplexFractalRigidMulti(FN_DECIMAL x, FN_DECIMAL y, FN_DECIMAL z)
    {
        int seed = m_seed;
        FN_DECIMAL sum = 1 - Math.Abs(SingleSimplex(seed, x, y, z));
        FN_DECIMAL amp = 1;

        for (int i = 1; i < m_octaves; i++)
        {
            x *= m_lacunarity;
            y *= m_lacunarity;
            z *= m_lacunarity;

            amp *= m_gain;
            sum -= (1 - Math.Abs(SingleSimplex(++seed, x, y, z))) * amp;
        }

        return sum;
    }

    public FN_DECIMAL GetSimplexFractal(FN_DECIMAL x, FN_DECIMAL y, FN_DECIMAL z, FN_DECIMAL w)
    {
        x *= m_frequency;
        y *= m_frequency;
        z *= m_frequency;
        w *= m_frequency;

        switch (m_fractalType)
        {
            case FractalType.FBM:
                return SingleSimplexFractalFBM(x, y, z, w);
            case FractalType.Billow:
                return SingleSimplexFractalBillow(x, y, z, w);
            case FractalType.RigidMulti:
                return SingleSimplexFractalRigidMulti(x, y, z, w);
            default:
                return 0;
        }
    }

    private FN_DECIMAL SingleSimplexFractalFBM(FN_DECIMAL x, FN_DECIMAL y, FN_DECIMAL z, FN_DECIMAL w)
    {
        int seed = m_seed;
        FN_DECIMAL sum = SingleSimplex(seed, x, y, z, w);
        FN_DECIMAL amp = 1;

        for (int i = 1; i < m_octaves; i++)
        {
            x *= m_lacunarity;
            y *= m_lacunarity;
            z *= m_lacunarity;
            w *= m_lacunarity;

            amp *= m_gain;
            sum += SingleSimplex(++seed, x, y, z, w) * amp;
        }

        return sum * m_fractalBounding;
    }

    private FN_DECIMAL SingleSimplexFractalBillow(FN_DECIMAL x, FN_DECIMAL y, FN_DECIMAL z, FN_DECIMAL w)
    {
        int seed = m_seed;
        FN_DECIMAL sum = Math.Abs(SingleSimplex(seed, x, y, z, w)) * 2 - 1;
        FN_DECIMAL amp = 1;

        for (int i = 1; i < m_octaves; i++)
        {
            x *= m_lacunarity;
            y *= m_lacunarity;
            z *= m_lacunarity;
            w *= m_lacunarity;

            amp *= m_gain;
            sum += (Math.Abs(SingleSimplex(++seed, x, y, z, w)) * 2 - 1) * amp;
        }

        return sum * m_fractalBounding;
    }

    private FN_DECIMAL SingleSimplexFractalRigidMulti(FN_DECIMAL x, FN_DECIMAL y, FN_DECIMAL z, FN_DECIMAL w)
    {
        int seed = m_seed;
        FN_DECIMAL sum = 1 - Math.Abs(SingleSimplex(seed, x, y, z, w));
        FN_DECIMAL amp = 1;

        for (int i = 1; i < m_octaves; i++)
        {
            x *= m_lacunarity;
            y *= m_lacunarity;
            z *= m_lacunarity;
            w *= m_lacunarity;

            amp *= m_gain;
            sum -= (1 - Math.Abs(SingleSimplex(++seed, x, y, z, w))) * amp;
        }

        return sum;
    }

    public FN_DECIMAL GetSimplex(FN_DECIMAL x, FN_DECIMAL y, FN_DECIMAL z)
    {
        return SingleSimplex(m_seed, x * m_frequency, y * m_frequency, z * m_frequency);
    }

    private const FN_DECIMAL F3 = (FN_DECIMAL) (1.0 / 3.0);
    private const FN_DECIMAL G3 = (FN_DECIMAL) (1.0 / 6.0);
    private const FN_DECIMAL G33 = G3 * 3 - 1;

    private FN_DECIMAL SingleSimplex(int seed, FN_DECIMAL x, FN_DECIMAL y, FN_DECIMAL z)
    {
        FN_DECIMAL t = (x + y + z) * F3;
        int i = FastFloor(x + t);
        int j = FastFloor(y + t);
        int k = FastFloor(z + t);

        t = (i + j + k) * G3;
        FN_DECIMAL x0 = x - (i - t);
        FN_DECIMAL y0 = y - (j - t);
        FN_DECIMAL z0 = z - (k - t);

        int i1, j1, k1;
        int i2, j2, k2;

        if (x0 >= y0)
        {
            if (y0 >= z0)
            {
                i1 = 1;
                j1 = 0;
                k1 = 0;
                i2 = 1;
                j2 = 1;
                k2 = 0;
            }
            else if (x0 >= z0)
            {
                i1 = 1;
                j1 = 0;
                k1 = 0;
                i2 = 1;
                j2 = 0;
                k2 = 1;
            }
            else // x0 < z0
            {
                i1 = 0;
                j1 = 0;
                k1 = 1;
                i2 = 1;
                j2 = 0;
                k2 = 1;
            }
        }
        else // x0 < y0
        {
            if (y0 < z0)
            {
                i1 = 0;
                j1 = 0;
                k1 = 1;
                i2 = 0;
                j2 = 1;
                k2 = 1;
            }
            else if (x0 < z0)
            {
                i1 = 0;
                j1 = 1;
                k1 = 0;
                i2 = 0;
                j2 = 1;
                k2 = 1;
            }
            else // x0 >= z0
            {
                i1 = 0;
                j1 = 1;
                k1 = 0;
                i2 = 1;
                j2 = 1;
                k2 = 0;
            }
        }

        FN_DECIMAL x1 = x0 - i1 + G3;
        FN_DECIMAL y1 = y0 - j1 + G3;
        FN_DECIMAL z1 = z0 - k1 + G3;
        FN_DECIMAL x2 = x0 - i2 + F3;
        FN_DECIMAL y2 = y0 - j2 + F3;
        FN_DECIMAL z2 = z0 - k2 + F3;
        FN_DECIMAL x3 = x0 + G33;
        FN_DECIMAL y3 = y0 + G33;
        FN_DECIMAL z3 = z0 + G33;

        FN_DECIMAL n0, n1, n2, n3;

        t = (FN_DECIMAL) 0.6 - x0 * x0 - y0 * y0 - z0 * z0;
        if (t < 0) n0 = 0;
        else
        {
            t *= t;
            n0 = t * t * GradCoord3D(seed, i, j, k, x0, y0, z0);
        }

        t = (FN_DECIMAL) 0.6 - x1 * x1 - y1 * y1 - z1 * z1;
        if (t < 0) n1 = 0;
        else
        {
            t *= t;
            n1 = t * t * GradCoord3D(seed, i + i1, j + j1, k + k1, x1, y1, z1);
        }

        t = (FN_DECIMAL) 0.6 - x2 * x2 - y2 * y2 - z2 * z2;
        if (t < 0) n2 = 0;
        else
        {
            t *= t;
            n2 = t * t * GradCoord3D(seed, i + i2, j + j2, k + k2, x2, y2, z2);
        }

        t = (FN_DECIMAL) 0.6 - x3 * x3 - y3 * y3 - z3 * z3;
        if (t < 0) n3 = 0;
        else
        {
            t *= t;
            n3 = t * t * GradCoord3D(seed, i + 1, j + 1, k + 1, x3, y3, z3);
        }

        return 32 * (n0 + n1 + n2 + n3);
    }

    public FN_DECIMAL GetSimplexFractal(FN_DECIMAL x, FN_DECIMAL y)
    {
        x *= m_frequency;
        y *= m_frequency;

        switch (m_fractalType)
        {
            case FractalType.FBM:
                return SingleSimplexFractalFBM(x, y);
            case FractalType.Billow:
                return SingleSimplexFractalBillow(x, y);
            case FractalType.RigidMulti:
                return SingleSimplexFractalRigidMulti(x, y);
            default:
                return 0;
        }
    }

    private FN_DECIMAL SingleSimplexFractalFBM(FN_DECIMAL x, FN_DECIMAL y)
    {
        int seed = m_seed;
        FN_DECIMAL sum = SingleSimplex(seed, x, y);
        FN_DECIMAL amp = 1;

        for (int i = 1; i < m_octaves; i++)
        {
            x *= m_lacunarity;
            y *= m_lacunarity;

            amp *= m_gain;
            sum += SingleSimplex(++seed, x, y) * amp;
        }

        return sum * m_fractalBounding;
    }

    private FN_DECIMAL SingleSimplexFractalBillow(FN_DECIMAL x, FN_DECIMAL y)
    {
        int seed = m_seed;
        FN_DECIMAL sum = Math.Abs(SingleSimplex(seed, x, y)) * 2 - 1;
        FN_DECIMAL amp = 1;

        for (int i = 1; i < m_octaves; i++)
        {
            x *= m_lacunarity;
            y *= m_lacunarity;

            amp *= m_gain;
            sum += (Math.Abs(SingleSimplex(++seed, x, y)) * 2 - 1) * amp;
        }

        return sum * m_fractalBounding;
    }

    private FN_DECIMAL SingleSimplexFractalRigidMulti(FN_DECIMAL x, FN_DECIMAL y)
    {
        int seed = m_seed;
        FN_DECIMAL sum = 1 - Math.Abs(SingleSimplex(seed, x, y));
        FN_DECIMAL amp = 1;

        for (int i = 1; i < m_octaves; i++)
        {
            x *= m_lacunarity;
            y *= m_lacunarity;

            amp *= m_gain;
            sum -= (1 - Math.Abs(SingleSimplex(++seed, x, y))) * amp;
        }

        return sum;
    }

    public FN_DECIMAL GetSimplex(FN_DECIMAL x, FN_DECIMAL y)
    {
        return SingleSimplex(m_seed, x * m_frequency, y * m_frequency);
    }

    private const FN_DECIMAL F2 = (FN_DECIMAL) (1.0 / 2.0);
    private const FN_DECIMAL G2 = (FN_DECIMAL) (1.0 / 4.0);

    private FN_DECIMAL SingleSimplex(int seed, FN_DECIMAL x, FN_DECIMAL y)
    {
        FN_DECIMAL t = (x + y) * F2;
        int i = FastFloor(x + t);
        int j = FastFloor(y + t);

        t = (i + j) * G2;
        FN_DECIMAL X0 = i - t;
        FN_DECIMAL Y0 = j - t;

        FN_DECIMAL x0 = x - X0;
        FN_DECIMAL y0 = y - Y0;

        int i1, j1;
        if (x0 > y0)
        {
            i1 = 1;
            j1 = 0;
        }
        else
        {
            i1 = 0;
            j1 = 1;
        }

        FN_DECIMAL x1 = x0 - i1 + G2;
        FN_DECIMAL y1 = y0 - j1 + G2;
        FN_DECIMAL x2 = x0 - 1 + F2;
        FN_DECIMAL y2 = y0 - 1 + F2;

        FN_DECIMAL n0, n1, n2;

        t = (FN_DECIMAL) 0.5 - x0 * x0 - y0 * y0;
        if (t < 0) n0 = 0;
        else
        {
            t *= t;
            n0 = t * t * GradCoord2D(seed, i, j, x0, y0);
        }

        t = (FN_DECIMAL) 0.5 - x1 * x1 - y1 * y1;
        if (t < 0) n1 = 0;
        else
        {
            t *= t;
            n1 = t * t * GradCoord2D(seed, i + i1, j + j1, x1, y1);
        }

        t = (FN_DECIMAL) 0.5 - x2 * x2 - y2 * y2;
        if (t < 0) n2 = 0;
        else
        {
            t *= t;
            n2 = t * t * GradCoord2D(seed, i + 1, j + 1, x2, y2);
        }

        return 50 * (n0 + n1 + n2);
    }

    public FN_DECIMAL GetSimplex(FN_DECIMAL x, FN_DECIMAL y, FN_DECIMAL z, FN_DECIMAL w)
    {
        return SingleSimplex(m_seed, x * m_frequency, y * m_frequency, z * m_frequency, w * m_frequency);
    }

    private static readonly byte[] SIMPLEX_4D =
    {
        0, 1, 2, 3, 0, 1, 3, 2, 0, 0, 0, 0, 0, 2, 3, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 2, 3, 0,
        0, 2, 1, 3, 0, 0, 0, 0, 0, 3, 1, 2, 0, 3, 2, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 3, 2, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        1, 2, 0, 3, 0, 0, 0, 0, 1, 3, 0, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 3, 0, 1, 2, 3, 1, 0,
        1, 0, 2, 3, 1, 0, 3, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 0, 3, 1, 0, 0, 0, 0, 2, 1, 3, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        2, 0, 1, 3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3, 0, 1, 2, 3, 0, 2, 1, 0, 0, 0, 0, 3, 1, 2, 0,
        2, 1, 0, 3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3, 1, 0, 2, 0, 0, 0, 0, 3, 2, 0, 1, 3, 2, 1, 0
    };

    private const FN_DECIMAL F4 = (FN_DECIMAL) ((2.23606797 - 1.0) / 4.0);
    private const FN_DECIMAL G4 = (FN_DECIMAL) ((5.0 - 2.23606797) / 20.0);

    private FN_DECIMAL SingleSimplex(int seed, FN_DECIMAL x, FN_DECIMAL y, FN_DECIMAL z, FN_DECIMAL w)
    {
        FN_DECIMAL n0, n1, n2, n3, n4;
        FN_DECIMAL t = (x + y + z + w) * F4;
        int i = FastFloor(x + t);
        int j = FastFloor(y + t);
        int k = FastFloor(z + t);
        int l = FastFloor(w + t);
        t = (i + j + k + l) * G4;
        FN_DECIMAL X0 = i - t;
        FN_DECIMAL Y0 = j - t;
        FN_DECIMAL Z0 = k - t;
        FN_DECIMAL W0 = l - t;
        FN_DECIMAL x0 = x - X0;
        FN_DECIMAL y0 = y - Y0;
        FN_DECIMAL z0 = z - Z0;
        FN_DECIMAL w0 = w - W0;

        int c = (x0 > y0) ? 32 : 0;
        c += (x0 > z0) ? 16 : 0;
        c += (y0 > z0) ? 8 : 0;
        c += (x0 > w0) ? 4 : 0;
        c += (y0 > w0) ? 2 : 0;
        c += (z0 > w0) ? 1 : 0;
        c <<= 2;

        int i1 = SIMPLEX_4D[c] >= 3 ? 1 : 0;
        int i2 = SIMPLEX_4D[c] >= 2 ? 1 : 0;
        int i3 = SIMPLEX_4D[c++] >= 1 ? 1 : 0;
        int j1 = SIMPLEX_4D[c] >= 3 ? 1 : 0;
        int j2 = SIMPLEX_4D[c] >= 2 ? 1 : 0;
        int j3 = SIMPLEX_4D[c++] >= 1 ? 1 : 0;
        int k1 = SIMPLEX_4D[c] >= 3 ? 1 : 0;
        int k2 = SIMPLEX_4D[c] >= 2 ? 1 : 0;
        int k3 = SIMPLEX_4D[c++] >= 1 ? 1 : 0;
        int l1 = SIMPLEX_4D[c] >= 3 ? 1 : 0;
        int l2 = SIMPLEX_4D[c] >= 2 ? 1 : 0;
        int l3 = SIMPLEX_4D[c] >= 1 ? 1 : 0;

        FN_DECIMAL x1 = x0 - i1 + G4;
        FN_DECIMAL y1 = y0 - j1 + G4;
        FN_DECIMAL z1 = z0 - k1 + G4;
        FN_DECIMAL w1 = w0 - l1 + G4;
        FN_DECIMAL x2 = x0 - i2 + 2 * G4;
        FN_DECIMAL y2 = y0 - j2 + 2 * G4;
        FN_DECIMAL z2 = z0 - k2 + 2 * G4;
        FN_DECIMAL w2 = w0 - l2 + 2 * G4;
        FN_DECIMAL x3 = x0 - i3 + 3 * G4;
        FN_DECIMAL y3 = y0 - j3 + 3 * G4;
        FN_DECIMAL z3 = z0 - k3 + 3 * G4;
        FN_DECIMAL w3 = w0 - l3 + 3 * G4;
        FN_DECIMAL x4 = x0 - 1 + 4 * G4;
        FN_DECIMAL y4 = y0 - 1 + 4 * G4;
        FN_DECIMAL z4 = z0 - 1 + 4 * G4;
        FN_DECIMAL w4 = w0 - 1 + 4 * G4;

        t = (FN_DECIMAL) 0.6 - x0 * x0 - y0 * y0 - z0 * z0 - w0 * w0;
        if (t < 0) n0 = 0;
        else
        {
            t *= t;
            n0 = t * t * GradCoord4D(seed, i, j, k, l, x0, y0, z0, w0);
        }

        t = (FN_DECIMAL) 0.6 - x1 * x1 - y1 * y1 - z1 * z1 - w1 * w1;
        if (t < 0) n1 = 0;
        else
        {
            t *= t;
            n1 = t * t * GradCoord4D(seed, i + i1, j + j1, k + k1, l + l1, x1, y1, z1, w1);
        }

        t = (FN_DECIMAL) 0.6 - x2 * x2 - y2 * y2 - z2 * z2 - w2 * w2;
        if (t < 0) n2 = 0;
        else
        {
            t *= t;
            n2 = t * t * GradCoord4D(seed, i + i2, j + j2, k + k2, l + l2, x2, y2, z2, w2);
        }

        t = (FN_DECIMAL) 0.6 - x3 * x3 - y3 * y3 - z3 * z3 - w3 * w3;
        if (t < 0) n3 = 0;
        else
        {
            t *= t;
            n3 = t * t * GradCoord4D(seed, i + i3, j + j3, k + k3, l + l3, x3, y3, z3, w3);
        }

        t = (FN_DECIMAL) 0.6 - x4 * x4 - y4 * y4 - z4 * z4 - w4 * w4;
        if (t < 0) n4 = 0;
        else
        {
            t *= t;
            n4 = t * t * GradCoord4D(seed, i + 1, j + 1, k + 1, l + 1, x4, y4, z4, w4);
        }

        return 27 * (n0 + n1 + n2 + n3 + n4);
    }

    // Cubic Noise
    public FN_DECIMAL GetCubicFractal(FN_DECIMAL x, FN_DECIMAL y, FN_DECIMAL z)
    {
        x *= m_frequency;
        y *= m_frequency;
        z *= m_frequency;

        switch (m_fractalType)
        {
            case FractalType.FBM:
                return SingleCubicFractalFBM(x, y, z);
            case FractalType.Billow:
                return SingleCubicFractalBillow(x, y, z);
            case FractalType.RigidMulti:
                return SingleCubicFractalRigidMulti(x, y, z);
            default:
                return 0;
        }
    }

    private FN_DECIMAL SingleCubicFractalFBM(FN_DECIMAL x, FN_DECIMAL y, FN_DECIMAL z)
    {
        int seed = m_seed;
        FN_DECIMAL sum = SingleCubic(seed, x, y, z);
        FN_DECIMAL amp = 1;
        int i = 0;

        while (++i < m_octaves)
        {
            x *= m_lacunarity;
            y *= m_lacunarity;
            z *= m_lacunarity;

            amp *= m_gain;
            sum += SingleCubic(++seed, x, y, z) * amp;
        }

        return sum * m_fractalBounding;
    }

    private FN_DECIMAL SingleCubicFractalBillow(FN_DECIMAL x, FN_DECIMAL y, FN_DECIMAL z)
    {
        int seed = m_seed;
        FN_DECIMAL sum = Math.Abs(SingleCubic(seed, x, y, z)) * 2 - 1;
        FN_DECIMAL amp = 1;
        int i = 0;

        while (++i < m_octaves)
        {
            x *= m_lacunarity;
            y *= m_lacunarity;
            z *= m_lacunarity;

            amp *= m_gain;
            sum += (Math.Abs(SingleCubic(++seed, x, y, z)) * 2 - 1) * amp;
        }

        return sum * m_fractalBounding;
    }

    private FN_DECIMAL SingleCubicFractalRigidMulti(FN_DECIMAL x, FN_DECIMAL y, FN_DECIMAL z)
    {
        int seed = m_seed;
        FN_DECIMAL sum = 1 - Math.Abs(SingleCubic(seed, x, y, z));
        FN_DECIMAL amp = 1;
        int i = 0;

        while (++i < m_octaves)
        {
            x *= m_lacunarity;
            y *= m_lacunarity;
            z *= m_lacunarity;

            amp *= m_gain;
            sum -= (1 - Math.Abs(SingleCubic(++seed, x, y, z))) * amp;
        }

        return sum;
    }

    public FN_DECIMAL GetCubic(FN_DECIMAL x, FN_DECIMAL y, FN_DECIMAL z)
    {
        return SingleCubic(m_seed, x * m_frequency, y * m_frequency, z * m_frequency);
    }

    private const FN_DECIMAL CUBIC_3D_BOUNDING = 1 / (FN_DECIMAL) (1.5 * 1.5 * 1.5);

    private FN_DECIMAL SingleCubic(int seed, FN_DECIMAL x, FN_DECIMAL y, FN_DECIMAL z)
    {
        int x1 = FastFloor(x);
        int y1 = FastFloor(y);
        int z1 = FastFloor(z);

        int x0 = x1 - 1;
        int y0 = y1 - 1;
        int z0 = z1 - 1;
        int x2 = x1 + 1;
        int y2 = y1 + 1;
        int z2 = z1 + 1;
        int x3 = x1 + 2;
        int y3 = y1 + 2;
        int z3 = z1 + 2;

        FN_DECIMAL xs = x - x1;
        FN_DECIMAL ys = y - y1;
        FN_DECIMAL zs = z - z1;

        return CubicLerp(
            CubicLerp(
                CubicLerp(ValCoord3D(seed, x0, y0, z0), ValCoord3D(seed, x1, y0, z0),
                    ValCoord3D(seed, x2, y0, z0), ValCoord3D(seed, x3, y0, z0), xs),
                CubicLerp(ValCoord3D(seed, x0, y1, z0), ValCoord3D(seed, x1, y1, z0),
                    ValCoord3D(seed, x2, y1, z0), ValCoord3D(seed, x3, y1, z0), xs),
                CubicLerp(ValCoord3D(seed, x0, y2, z0), ValCoord3D(seed, x1, y2, z0),
                    ValCoord3D(seed, x2, y2, z0), ValCoord3D(seed, x3, y2, z0), xs),
                CubicLerp(ValCoord3D(seed, x0, y3, z0), ValCoord3D(seed, x1, y3, z0),
                    ValCoord3D(seed, x2, y3, z0), ValCoord3D(seed, x3, y3, z0), xs),
                ys),
            CubicLerp(
                CubicLerp(ValCoord3D(seed, x0, y0, z1), ValCoord3D(seed, x1, y0, z1),
                    ValCoord3D(seed, x2, y0, z1), ValCoord3D(seed, x3, y0, z1), xs),
                CubicLerp(ValCoord3D(seed, x0, y1, z1), ValCoord3D(seed, x1, y1, z1),
                    ValCoord3D(seed, x2, y1, z1), ValCoord3D(seed, x3, y1, z1), xs),
                CubicLerp(ValCoord3D(seed, x0, y2, z1), ValCoord3D(seed, x1, y2, z1),
                    ValCoord3D(seed, x2, y2, z1), ValCoord3D(seed, x3, y2, z1), xs),
                CubicLerp(ValCoord3D(seed, x0, y3, z1), ValCoord3D(seed, x1, y3, z1),
                    ValCoord3D(seed, x2, y3, z1), ValCoord3D(seed, x3, y3, z1), xs),
                ys),
            CubicLerp(
                CubicLerp(ValCoord3D(seed, x0, y0, z2), ValCoord3D(seed, x1, y0, z2),
                    ValCoord3D(seed, x2, y0, z2), ValCoord3D(seed, x3, y0, z2), xs),
                CubicLerp(ValCoord3D(seed, x0, y1, z2), ValCoord3D(seed, x1, y1, z2),
                    ValCoord3D(seed, x2, y1, z2), ValCoord3D(seed, x3, y1, z2), xs),
                CubicLerp(ValCoord3D(seed, x0, y2, z2), ValCoord3D(seed, x1, y2, z2),
                    ValCoord3D(seed, x2, y2, z2), ValCoord3D(seed, x3, y2, z2), xs),
                CubicLerp(ValCoord3D(seed, x0, y3, z2), ValCoord3D(seed, x1, y3, z2),
                    ValCoord3D(seed, x2, y3, z2), ValCoord3D(seed, x3, y3, z2), xs),
                ys),
            CubicLerp(
                CubicLerp(ValCoord3D(seed, x0, y0, z3), ValCoord3D(seed, x1, y0, z3),
                    ValCoord3D(seed, x2, y0, z3), ValCoord3D(seed, x3, y0, z3), xs),
                CubicLerp(ValCoord3D(seed, x0, y1, z3), ValCoord3D(seed, x1, y1, z3),
                    ValCoord3D(seed, x2, y1, z3), ValCoord3D(seed, x3, y1, z3), xs),
                CubicLerp(ValCoord3D(seed, x0, y2, z3), ValCoord3D(seed, x1, y2, z3),
                    ValCoord3D(seed, x2, y2, z3), ValCoord3D(seed, x3, y2, z3), xs),
                CubicLerp(ValCoord3D(seed, x0, y3, z3), ValCoord3D(seed, x1, y3, z3),
                    ValCoord3D(seed, x2, y3, z3), ValCoord3D(seed, x3, y3, z3), xs),
                ys),
            zs) * CUBIC_3D_BOUNDING;
    }


    public FN_DECIMAL GetCubicFractal(FN_DECIMAL x, FN_DECIMAL y)
    {
        x *= m_frequency;
        y *= m_frequency;

        switch (m_fractalType)
        {
            case FractalType.FBM:
                return SingleCubicFractalFBM(x, y);
            case FractalType.Billow:
                return SingleCubicFractalBillow(x, y);
            case FractalType.RigidMulti:
                return SingleCubicFractalRigidMulti(x, y);
            default:
                return 0;
        }
    }

    private FN_DECIMAL SingleCubicFractalFBM(FN_DECIMAL x, FN_DECIMAL y)
    {
        int seed = m_seed;
        FN_DECIMAL sum = SingleCubic(seed, x, y);
        FN_DECIMAL amp = 1;
        int i = 0;

        while (++i < m_octaves)
        {
            x *= m_lacunarity;
            y *= m_lacunarity;

            amp *= m_gain;
            sum += SingleCubic(++seed, x, y) * amp;
        }

        return sum * m_fractalBounding;
    }

    private FN_DECIMAL SingleCubicFractalBillow(FN_DECIMAL x, FN_DECIMAL y)
    {
        int seed = m_seed;
        FN_DECIMAL sum = Math.Abs(SingleCubic(seed, x, y)) * 2 - 1;
        FN_DECIMAL amp = 1;
        int i = 0;

        while (++i < m_octaves)
        {
            x *= m_lacunarity;
            y *= m_lacunarity;

            amp *= m_gain;
            sum += (Math.Abs(SingleCubic(++seed, x, y)) * 2 - 1) * amp;
        }

        return sum * m_fractalBounding;
    }

    private FN_DECIMAL SingleCubicFractalRigidMulti(FN_DECIMAL x, FN_DECIMAL y)
    {
        int seed = m_seed;
        FN_DECIMAL sum = 1 - Math.Abs(SingleCubic(seed, x, y));
        FN_DECIMAL amp = 1;
        int i = 0;

        while (++i < m_octaves)
        {
            x *= m_lacunarity;
            y *= m_lacunarity;

            amp *= m_gain;
            sum -= (1 - Math.Abs(SingleCubic(++seed, x, y))) * amp;
        }

        return sum;
    }

    public FN_DECIMAL GetCubic(FN_DECIMAL x, FN_DECIMAL y)
    {
        x *= m_frequency;
        y *= m_frequency;

        return SingleCubic(0, x, y);
    }

    private const FN_DECIMAL CUBIC_2D_BOUNDING = 1 / (FN_DECIMAL) (1.5 * 1.5);

    private FN_DECIMAL SingleCubic(int seed, FN_DECIMAL x, FN_DECIMAL y)
    {
        int x1 = FastFloor(x);
        int y1 = FastFloor(y);

        int x0 = x1 - 1;
        int y0 = y1 - 1;
        int x2 = x1 + 1;
        int y2 = y1 + 1;
        int x3 = x1 + 2;
        int y3 = y1 + 2;

        FN_DECIMAL xs = x - x1;
        FN_DECIMAL ys = y - y1;

        return CubicLerp(
            CubicLerp(ValCoord2D(seed, x0, y0), ValCoord2D(seed, x1, y0), ValCoord2D(seed, x2, y0),
                ValCoord2D(seed, x3, y0),
                xs),
            CubicLerp(ValCoord2D(seed, x0, y1), ValCoord2D(seed, x1, y1), ValCoord2D(seed, x2, y1),
                ValCoord2D(seed, x3, y1),
                xs),
            CubicLerp(ValCoord2D(seed, x0, y2), ValCoord2D(seed, x1, y2), ValCoord2D(seed, x2, y2),
                ValCoord2D(seed, x3, y2),
                xs),
            CubicLerp(ValCoord2D(seed, x0, y3), ValCoord2D(seed, x1, y3), ValCoord2D(seed, x2, y3),
                ValCoord2D(seed, x3, y3),
                xs),
            ys) * CUBIC_2D_BOUNDING;
    }

    // Cellular Noise
    public FN_DECIMAL GetCellular(FN_DECIMAL x, FN_DECIMAL y, FN_DECIMAL z)
    {
        x *= m_frequency;
        y *= m_frequency;
        z *= m_frequency;

        switch (m_cellularReturnType)
        {
            case CellularReturnType.CellValue:
            case CellularReturnType.NoiseLookup:
            case CellularReturnType.Distance:
                return SingleCellular(x, y, z);
            default:
                return SingleCellular2Edge(x, y, z);
        }
    }

    private FN_DECIMAL SingleCellular(FN_DECIMAL x, FN_DECIMAL y, FN_DECIMAL z)
    {
        int xr = FastRound(x);
        int yr = FastRound(y);
        int zr = FastRound(z);

        FN_DECIMAL distance = 999999;
        int xc = 0, yc = 0, zc = 0;

        switch (m_cellularDistanceFunction)
        {
            case CellularDistanceFunction.Euclidean:
                for (int xi = xr - 1; xi <= xr + 1; xi++)
                {
                    for (int yi = yr - 1; yi <= yr + 1; yi++)
                    {
                        for (int zi = zr - 1; zi <= zr + 1; zi++)
                        {
                            Float3 vec = CELL_3D[Hash3D(m_seed, xi, yi, zi) & 255];

                            FN_DECIMAL vecX = xi - x + vec.x * m_cellularJitter;
                            FN_DECIMAL vecY = yi - y + vec.y * m_cellularJitter;
                            FN_DECIMAL vecZ = zi - z + vec.z * m_cellularJitter;

                            FN_DECIMAL newDistance = vecX * vecX + vecY * vecY + vecZ * vecZ;

                            if (newDistance < distance)
                            {
                                distance = newDistance;
                                xc = xi;
                                yc = yi;
                                zc = zi;
                            }
                        }
                    }
                }

                break;
            case CellularDistanceFunction.Manhattan:
                for (int xi = xr - 1; xi <= xr + 1; xi++)
                {
                    for (int yi = yr - 1; yi <= yr + 1; yi++)
                    {
                        for (int zi = zr - 1; zi <= zr + 1; zi++)
                        {
                            Float3 vec = CELL_3D[Hash3D(m_seed, xi, yi, zi) & 255];

                            FN_DECIMAL vecX = xi - x + vec.x * m_cellularJitter;
                            FN_DECIMAL vecY = yi - y + vec.y * m_cellularJitter;
                            FN_DECIMAL vecZ = zi - z + vec.z * m_cellularJitter;

                            FN_DECIMAL newDistance = Math.Abs(vecX) + Math.Abs(vecY) + Math.Abs(vecZ);

                            if (newDistance < distance)
                            {
                                distance = newDistance;
                                xc = xi;
                                yc = yi;
                                zc = zi;
                            }
                        }
                    }
                }

                break;
            case CellularDistanceFunction.Natural:
                for (int xi = xr - 1; xi <= xr + 1; xi++)
                {
                    for (int yi = yr - 1; yi <= yr + 1; yi++)
                    {
                        for (int zi = zr - 1; zi <= zr + 1; zi++)
                        {
                            Float3 vec = CELL_3D[Hash3D(m_seed, xi, yi, zi) & 255];

                            FN_DECIMAL vecX = xi - x + vec.x * m_cellularJitter;
                            FN_DECIMAL vecY = yi - y + vec.y * m_cellularJitter;
                            FN_DECIMAL vecZ = zi - z + vec.z * m_cellularJitter;

                            FN_DECIMAL newDistance = (Math.Abs(vecX) + Math.Abs(vecY) + Math.Abs(vecZ)) +
                                                     (vecX * vecX + vecY * vecY + vecZ * vecZ);

                            if (newDistance < distance)
                            {
                                distance = newDistance;
                                xc = xi;
                                yc = yi;
                                zc = zi;
                            }
                        }
                    }
                }

                break;
        }

        switch (m_cellularReturnType)
        {
            case CellularReturnType.CellValue:
                return ValCoord3D(m_seed, xc, yc, zc);

            case CellularReturnType.NoiseLookup:
                Float3 vec = CELL_3D[Hash3D(m_seed, xc, yc, zc) & 255];
                return m_cellularNoiseLookup.GetNoise(xc + vec.x * m_cellularJitter, yc + vec.y * m_cellularJitter,
                    zc + vec.z * m_cellularJitter);

            case CellularReturnType.Distance:
                return distance;
            default:
                return 0;
        }
    }

    private FN_DECIMAL SingleCellular2Edge(FN_DECIMAL x, FN_DECIMAL y, FN_DECIMAL z)
    {
        int xr = FastRound(x);
        int yr = FastRound(y);
        int zr = FastRound(z);

        FN_DECIMAL[] distance = {999999, 999999, 999999, 999999};

        switch (m_cellularDistanceFunction)
        {
            case CellularDistanceFunction.Euclidean:
                for (int xi = xr - 1; xi <= xr + 1; xi++)
                {
                    for (int yi = yr - 1; yi <= yr + 1; yi++)
                    {
                        for (int zi = zr - 1; zi <= zr + 1; zi++)
                        {
                            Float3 vec = CELL_3D[Hash3D(m_seed, xi, yi, zi) & 255];

                            FN_DECIMAL vecX = xi - x + vec.x * m_cellularJitter;
                            FN_DECIMAL vecY = yi - y + vec.y * m_cellularJitter;
                            FN_DECIMAL vecZ = zi - z + vec.z * m_cellularJitter;

                            FN_DECIMAL newDistance = vecX * vecX + vecY * vecY + vecZ * vecZ;

                            for (int i = m_cellularDistanceIndex1; i > 0; i--)
                                distance[i] = Math.Max(Math.Min(distance[i], newDistance), distance[i - 1]);
                            distance[0] = Math.Min(distance[0], newDistance);
                        }
                    }
                }

                break;
            case CellularDistanceFunction.Manhattan:
                for (int xi = xr - 1; xi <= xr + 1; xi++)
                {
                    for (int yi = yr - 1; yi <= yr + 1; yi++)
                    {
                        for (int zi = zr - 1; zi <= zr + 1; zi++)
                        {
                            Float3 vec = CELL_3D[Hash3D(m_seed, xi, yi, zi) & 255];

                            FN_DECIMAL vecX = xi - x + vec.x * m_cellularJitter;
                            FN_DECIMAL vecY = yi - y + vec.y * m_cellularJitter;
                            FN_DECIMAL vecZ = zi - z + vec.z * m_cellularJitter;

                            FN_DECIMAL newDistance = Math.Abs(vecX) + Math.Abs(vecY) + Math.Abs(vecZ);

                            for (int i = m_cellularDistanceIndex1; i > 0; i--)
                                distance[i] = Math.Max(Math.Min(distance[i], newDistance), distance[i - 1]);
                            distance[0] = Math.Min(distance[0], newDistance);
                        }
                    }
                }

                break;
            case CellularDistanceFunction.Natural:
                for (int xi = xr - 1; xi <= xr + 1; xi++)
                {
                    for (int yi = yr - 1; yi <= yr + 1; yi++)
                    {
                        for (int zi = zr - 1; zi <= zr + 1; zi++)
                        {
                            Float3 vec = CELL_3D[Hash3D(m_seed, xi, yi, zi) & 255];

                            FN_DECIMAL vecX = xi - x + vec.x * m_cellularJitter;
                            FN_DECIMAL vecY = yi - y + vec.y * m_cellularJitter;
                            FN_DECIMAL vecZ = zi - z + vec.z * m_cellularJitter;

                            FN_DECIMAL newDistance = (Math.Abs(vecX) + Math.Abs(vecY) + Math.Abs(vecZ)) +
                                                     (vecX * vecX + vecY * vecY + vecZ * vecZ);

                            for (int i = m_cellularDistanceIndex1; i > 0; i--)
                                distance[i] = Math.Max(Math.Min(distance[i], newDistance), distance[i - 1]);
                            distance[0] = Math.Min(distance[0], newDistance);
                        }
                    }
                }

                break;
            default:
                break;
        }

        switch (m_cellularReturnType)
        {
            case CellularReturnType.Distance2:
                return distance[m_cellularDistanceIndex1];
            case CellularReturnType.Distance2Add:
                return distance[m_cellularDistanceIndex1] + distance[m_cellularDistanceIndex0];
            case CellularReturnType.Distance2Sub:
                return distance[m_cellularDistanceIndex1] - distance[m_cellularDistanceIndex0];
            case CellularReturnType.Distance2Mul:
                return distance[m_cellularDistanceIndex1] * distance[m_cellularDistanceIndex0];
            case CellularReturnType.Distance2Div:
                return distance[m_cellularDistanceIndex0] / distance[m_cellularDistanceIndex1];
            default:
                return 0;
        }
    }

    public FN_DECIMAL GetCellular(FN_DECIMAL x, FN_DECIMAL y)
    {
        x *= m_frequency;
        y *= m_frequency;

        switch (m_cellularReturnType)
        {
            case CellularReturnType.CellValue:
            case CellularReturnType.NoiseLookup:
            case CellularReturnType.Distance:
                return SingleCellular(x, y);
            default:
                return SingleCellular2Edge(x, y);
        }
    }

    private FN_DECIMAL SingleCellular(FN_DECIMAL x, FN_DECIMAL y)
    {
        int xr = FastRound(x);
        int yr = FastRound(y);

        FN_DECIMAL distance = 999999;
        int xc = 0, yc = 0;

        switch (m_cellularDistanceFunction)
        {
            default:
            case CellularDistanceFunction.Euclidean:
                for (int xi = xr - 1; xi <= xr + 1; xi++)
                {
                    for (int yi = yr - 1; yi <= yr + 1; yi++)
                    {
                        Float2 vec = CELL_2D[Hash2D(m_seed, xi, yi) & 255];

                        FN_DECIMAL vecX = xi - x + vec.x * m_cellularJitter;
                        FN_DECIMAL vecY = yi - y + vec.y * m_cellularJitter;

                        FN_DECIMAL newDistance = vecX * vecX + vecY * vecY;

                        if (newDistance < distance)
                        {
                            distance = newDistance;
                            xc = xi;
                            yc = yi;
                        }
                    }
                }

                break;
            case CellularDistanceFunction.Manhattan:
                for (int xi = xr - 1; xi <= xr + 1; xi++)
                {
                    for (int yi = yr - 1; yi <= yr + 1; yi++)
                    {
                        Float2 vec = CELL_2D[Hash2D(m_seed, xi, yi) & 255];

                        FN_DECIMAL vecX = xi - x + vec.x * m_cellularJitter;
                        FN_DECIMAL vecY = yi - y + vec.y * m_cellularJitter;

                        FN_DECIMAL newDistance = (Math.Abs(vecX) + Math.Abs(vecY));

                        if (newDistance < distance)
                        {
                            distance = newDistance;
                            xc = xi;
                            yc = yi;
                        }
                    }
                }

                break;
            case CellularDistanceFunction.Natural:
                for (int xi = xr - 1; xi <= xr + 1; xi++)
                {
                    for (int yi = yr - 1; yi <= yr + 1; yi++)
                    {
                        Float2 vec = CELL_2D[Hash2D(m_seed, xi, yi) & 255];

                        FN_DECIMAL vecX = xi - x + vec.x * m_cellularJitter;
                        FN_DECIMAL vecY = yi - y + vec.y * m_cellularJitter;

                        FN_DECIMAL newDistance = (Math.Abs(vecX) + Math.Abs(vecY)) + (vecX * vecX + vecY * vecY);

                        if (newDistance < distance)
                        {
                            distance = newDistance;
                            xc = xi;
                            yc = yi;
                        }
                    }
                }

                break;
        }

        switch (m_cellularReturnType)
        {
            case CellularReturnType.CellValue:
                return ValCoord2D(m_seed, xc, yc);

            case CellularReturnType.NoiseLookup:
                Float2 vec = CELL_2D[Hash2D(m_seed, xc, yc) & 255];
                return m_cellularNoiseLookup.GetNoise(xc + vec.x * m_cellularJitter, yc + vec.y * m_cellularJitter);

            case CellularReturnType.Distance:
                return distance;
            default:
                return 0;
        }
    }

    private FN_DECIMAL SingleCellular2Edge(FN_DECIMAL x, FN_DECIMAL y)
    {
        int xr = FastRound(x);
        int yr = FastRound(y);

        FN_DECIMAL[] distance = {999999, 999999, 999999, 999999};

        switch (m_cellularDistanceFunction)
        {
            default:
            case CellularDistanceFunction.Euclidean:
                for (int xi = xr - 1; xi <= xr + 1; xi++)
                {
                    for (int yi = yr - 1; yi <= yr + 1; yi++)
                    {
                        Float2 vec = CELL_2D[Hash2D(m_seed, xi, yi) & 255];

                        FN_DECIMAL vecX = xi - x + vec.x * m_cellularJitter;
                        FN_DECIMAL vecY = yi - y + vec.y * m_cellularJitter;

                        FN_DECIMAL newDistance = vecX * vecX + vecY * vecY;

                        for (int i = m_cellularDistanceIndex1; i > 0; i--)
                            distance[i] = Math.Max(Math.Min(distance[i], newDistance), distance[i - 1]);
                        distance[0] = Math.Min(distance[0], newDistance);
                    }
                }

                break;
            case CellularDistanceFunction.Manhattan:
                for (int xi = xr - 1; xi <= xr + 1; xi++)
                {
                    for (int yi = yr - 1; yi <= yr + 1; yi++)
                    {
                        Float2 vec = CELL_2D[Hash2D(m_seed, xi, yi) & 255];

                        FN_DECIMAL vecX = xi - x + vec.x * m_cellularJitter;
                        FN_DECIMAL vecY = yi - y + vec.y * m_cellularJitter;

                        FN_DECIMAL newDistance = Math.Abs(vecX) + Math.Abs(vecY);

                        for (int i = m_cellularDistanceIndex1; i > 0; i--)
                            distance[i] = Math.Max(Math.Min(distance[i], newDistance), distance[i - 1]);
                        distance[0] = Math.Min(distance[0], newDistance);
                    }
                }

                break;
            case CellularDistanceFunction.Natural:
                for (int xi = xr - 1; xi <= xr + 1; xi++)
                {
                    for (int yi = yr - 1; yi <= yr + 1; yi++)
                    {
                        Float2 vec = CELL_2D[Hash2D(m_seed, xi, yi) & 255];

                        FN_DECIMAL vecX = xi - x + vec.x * m_cellularJitter;
                        FN_DECIMAL vecY = yi - y + vec.y * m_cellularJitter;

                        FN_DECIMAL newDistance = (Math.Abs(vecX) + Math.Abs(vecY)) + (vecX * vecX + vecY * vecY);

                        for (int i = m_cellularDistanceIndex1; i > 0; i--)
                            distance[i] = Math.Max(Math.Min(distance[i], newDistance), distance[i - 1]);
                        distance[0] = Math.Min(distance[0], newDistance);
                    }
                }

                break;
        }

        switch (m_cellularReturnType)
        {
            case CellularReturnType.Distance2:
                return distance[m_cellularDistanceIndex1];
            case CellularReturnType.Distance2Add:
                return distance[m_cellularDistanceIndex1] + distance[m_cellularDistanceIndex0];
            case CellularReturnType.Distance2Sub:
                return distance[m_cellularDistanceIndex1] - distance[m_cellularDistanceIndex0];
            case CellularReturnType.Distance2Mul:
                return distance[m_cellularDistanceIndex1] * distance[m_cellularDistanceIndex0];
            case CellularReturnType.Distance2Div:
                return distance[m_cellularDistanceIndex0] / distance[m_cellularDistanceIndex1];
            default:
                return 0;
        }
    }

    public void GradientPerturb(ref FN_DECIMAL x, ref FN_DECIMAL y, ref FN_DECIMAL z)
    {
        SingleGradientPerturb(m_seed, m_gradientPerturbAmp, m_frequency, ref x, ref y, ref z);
    }

    public void GradientPerturbFractal(ref FN_DECIMAL x, ref FN_DECIMAL y, ref FN_DECIMAL z)
    {
        int seed = m_seed;
        FN_DECIMAL amp = m_gradientPerturbAmp * m_fractalBounding;
        FN_DECIMAL freq = m_frequency;

        SingleGradientPerturb(seed, amp, m_frequency, ref x, ref y, ref z);

        for (int i = 1; i < m_octaves; i++)
        {
            freq *= m_lacunarity;
            amp *= m_gain;
            SingleGradientPerturb(++seed, amp, freq, ref x, ref y, ref z);
        }
    }

    private void SingleGradientPerturb(int seed, FN_DECIMAL perturbAmp, FN_DECIMAL frequency, ref FN_DECIMAL x,
        ref FN_DECIMAL y, ref FN_DECIMAL z)
    {
        FN_DECIMAL xf = x * frequency;
        FN_DECIMAL yf = y * frequency;
        FN_DECIMAL zf = z * frequency;

        int x0 = FastFloor(xf);
        int y0 = FastFloor(yf);
        int z0 = FastFloor(zf);
        int x1 = x0 + 1;
        int y1 = y0 + 1;
        int z1 = z0 + 1;

        FN_DECIMAL xs, ys, zs;
        switch (m_interp)
        {
            default:
            case Interp.Linear:
                xs = xf - x0;
                ys = yf - y0;
                zs = zf - z0;
                break;
            case Interp.Hermite:
                xs = InterpHermiteFunc(xf - x0);
                ys = InterpHermiteFunc(yf - y0);
                zs = InterpHermiteFunc(zf - z0);
                break;
            case Interp.Quintic:
                xs = InterpQuinticFunc(xf - x0);
                ys = InterpQuinticFunc(yf - y0);
                zs = InterpQuinticFunc(zf - z0);
                break;
        }

        Float3 vec0 = CELL_3D[Hash3D(seed, x0, y0, z0) & 255];
        Float3 vec1 = CELL_3D[Hash3D(seed, x1, y0, z0) & 255];

        FN_DECIMAL lx0x = Lerp(vec0.x, vec1.x, xs);
        FN_DECIMAL ly0x = Lerp(vec0.y, vec1.y, xs);
        FN_DECIMAL lz0x = Lerp(vec0.z, vec1.z, xs);

        vec0 = CELL_3D[Hash3D(seed, x0, y1, z0) & 255];
        vec1 = CELL_3D[Hash3D(seed, x1, y1, z0) & 255];

        FN_DECIMAL lx1x = Lerp(vec0.x, vec1.x, xs);
        FN_DECIMAL ly1x = Lerp(vec0.y, vec1.y, xs);
        FN_DECIMAL lz1x = Lerp(vec0.z, vec1.z, xs);

        FN_DECIMAL lx0y = Lerp(lx0x, lx1x, ys);
        FN_DECIMAL ly0y = Lerp(ly0x, ly1x, ys);
        FN_DECIMAL lz0y = Lerp(lz0x, lz1x, ys);

        vec0 = CELL_3D[Hash3D(seed, x0, y0, z1) & 255];
        vec1 = CELL_3D[Hash3D(seed, x1, y0, z1) & 255];

        lx0x = Lerp(vec0.x, vec1.x, xs);
        ly0x = Lerp(vec0.y, vec1.y, xs);
        lz0x = Lerp(vec0.z, vec1.z, xs);

        vec0 = CELL_3D[Hash3D(seed, x0, y1, z1) & 255];
        vec1 = CELL_3D[Hash3D(seed, x1, y1, z1) & 255];

        lx1x = Lerp(vec0.x, vec1.x, xs);
        ly1x = Lerp(vec0.y, vec1.y, xs);
        lz1x = Lerp(vec0.z, vec1.z, xs);

        x += Lerp(lx0y, Lerp(lx0x, lx1x, ys), zs) * perturbAmp;
        y += Lerp(ly0y, Lerp(ly0x, ly1x, ys), zs) * perturbAmp;
        z += Lerp(lz0y, Lerp(lz0x, lz1x, ys), zs) * perturbAmp;
    }

    public void GradientPerturb(ref FN_DECIMAL x, ref FN_DECIMAL y)
    {
        SingleGradientPerturb(m_seed, m_gradientPerturbAmp, m_frequency, ref x, ref y);
    }

    public void GradientPerturbFractal(ref FN_DECIMAL x, ref FN_DECIMAL y)
    {
        int seed = m_seed;
        FN_DECIMAL amp = m_gradientPerturbAmp * m_fractalBounding;
        FN_DECIMAL freq = m_frequency;

        SingleGradientPerturb(seed, amp, m_frequency, ref x, ref y);

        for (int i = 1; i < m_octaves; i++)
        {
            freq *= m_lacunarity;
            amp *= m_gain;
            SingleGradientPerturb(++seed, amp, freq, ref x, ref y);
        }
    }

    private void SingleGradientPerturb(int seed, FN_DECIMAL perturbAmp, FN_DECIMAL frequency, ref FN_DECIMAL x,
        ref FN_DECIMAL y)
    {
        FN_DECIMAL xf = x * frequency;
        FN_DECIMAL yf = y * frequency;

        int x0 = FastFloor(xf);
        int y0 = FastFloor(yf);
        int x1 = x0 + 1;
        int y1 = y0 + 1;

        FN_DECIMAL xs, ys;
        switch (m_interp)
        {
            default:
            case Interp.Linear:
                xs = xf - x0;
                ys = yf - y0;
                break;
            case Interp.Hermite:
                xs = InterpHermiteFunc(xf - x0);
                ys = InterpHermiteFunc(yf - y0);
                break;
            case Interp.Quintic:
                xs = InterpQuinticFunc(xf - x0);
                ys = InterpQuinticFunc(yf - y0);
                break;
        }

        Float2 vec0 = CELL_2D[Hash2D(seed, x0, y0) & 255];
        Float2 vec1 = CELL_2D[Hash2D(seed, x1, y0) & 255];

        FN_DECIMAL lx0x = Lerp(vec0.x, vec1.x, xs);
        FN_DECIMAL ly0x = Lerp(vec0.y, vec1.y, xs);

        vec0 = CELL_2D[Hash2D(seed, x0, y1) & 255];
        vec1 = CELL_2D[Hash2D(seed, x1, y1) & 255];

        FN_DECIMAL lx1x = Lerp(vec0.x, vec1.x, xs);
        FN_DECIMAL ly1x = Lerp(vec0.y, vec1.y, xs);

        x += Lerp(lx0x, lx1x, ys) * perturbAmp;
        y += Lerp(ly0x, ly1x, ys) * perturbAmp;
    }
}
