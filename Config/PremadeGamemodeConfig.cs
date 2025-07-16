using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace CustomGameModes.Config;

[Serializable]
public class PremadeGamemodeConfig
{
    public string assetBundle;
    public string gamemodeName;
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string author;
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string category;
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public List<string> capsuleArts;
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public List<string> screenArts;
}