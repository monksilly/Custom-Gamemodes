using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace CustomGameModes.Config;

[Serializable]
public class GamemodeConfig
{
    public string gamemodeName;
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string author;
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string category;
    public string introText;
    public bool isEndless;
    public bool hasPerks;
    public bool hasRevives;
    public string capsuleIcon;         // e.g. "capsule.png"
    public string screenIcon;          // e.g. "screen.png"
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string assetBundleFileName; // e.g. "atoms experimental levels" (no path or extension)
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string gameType;
    public List<RegionConfig> regions;
}