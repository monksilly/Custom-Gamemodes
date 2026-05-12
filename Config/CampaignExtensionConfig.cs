using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace CustomGameModes.Config;

[Serializable]
public class CampaignExtensionConfig
{
    public string extensionName;
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string author;
    public string capsuleIcon;         // e.g. "capsule.png"
    public string screenIcon;          // e.g. "screen.png"
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string assetBundleFileName; // e.g. "atoms experimental levels" (no path or extension)
    public List<RegionConfig> regions;
}