using System;
using System.Collections.Generic;

namespace CustomGameModes.Config;

[Serializable]
public class RegionConfig
{
    public string regionName;
    public List<SubregionConfig> subregions;
}