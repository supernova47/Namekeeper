using System;

namespace SuperNova.ValheimNamekeeper
{
    internal static class MapAdminAccess
    {
        internal static bool Authorize(bool server, bool ready, bool connected,
            string authenticatedTransportId, Func<string, bool> nativeAdminCheck)
        {
            return server && ready && connected && !string.IsNullOrEmpty(authenticatedTransportId) &&
                nativeAdminCheck != null && nativeAdminCheck(authenticatedTransportId);
        }
    }
}
