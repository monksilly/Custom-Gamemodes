using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace CustomGameModes.Config;

[Serializable]
public class SubregionConfig
{
    public string subregionName;
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string levelNameContains;

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public List<string> levels;
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public List<string> blacklist;
}