using Newtonsoft.Json;

namespace BatRun
{
    public class BezelInfo
    {
        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }

        [JsonProperty("top")]
        public int Top { get; set; }

        [JsonProperty("left")]
        public int Left { get; set; }

        [JsonProperty("bottom")]
        public int Bottom { get; set; }

        [JsonProperty("right")]
        public int Right { get; set; }
    }
}
