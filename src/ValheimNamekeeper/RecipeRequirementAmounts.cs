namespace SuperNova.ValheimNamekeeper
{
    internal static class RecipeRequirementAmounts
    {
        internal static bool TryApply(Piece.Requirement[] requirements, int[] amounts)
        {
            if (requirements == null || amounts == null || requirements.Length != amounts.Length)
                return false;

            // Validate the complete recipe before changing it, so an incomplete prefab
            // cannot leave a half-updated Hammer entry.
            for (int i = 0; i < requirements.Length; i++)
            {
                if (requirements[i] == null || requirements[i].m_resItem == null)
                    return false;
            }

            for (int i = 0; i < requirements.Length; i++)
                requirements[i].m_amount = amounts[i];

            return true;
        }
    }
}
