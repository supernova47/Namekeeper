namespace SuperNova.ValheimNamekeeper
{
    internal static class CampFootprint
    {
        // Clutter candidates have Y=0 before their ground raycast. Height must not
        // participate in the clearing test, even for camps high above sea level.
        internal static bool Contains(float x, float z, float centerX, float centerZ, float radius)
        {
            float dx = x - centerX;
            float dz = z - centerZ;
            return dx * dx + dz * dz <= radius * radius;
        }
    }
}
