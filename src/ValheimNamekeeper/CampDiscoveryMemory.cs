using System.Collections.Generic;
using System.Globalization;

namespace SuperNova.ValheimNamekeeper
{
    internal static class CampDiscoveryMemory
    {
        internal static bool Remember(IDictionary<string, string> data, long world, int x, int z)
        {
            // Character custom data is saved by Valheim. Scope by world and horizontal
            // position, not shared shop name, language, session, or terrain height.
            string key = "sn_namekeeper_discovery:" + world.ToString(CultureInfo.InvariantCulture) + ":" +
                x.ToString(CultureInfo.InvariantCulture) + ":" + z.ToString(CultureInfo.InvariantCulture);
            if (data.ContainsKey(key)) return false;
            data[key] = "1";
            return true;
        }
    }
}
