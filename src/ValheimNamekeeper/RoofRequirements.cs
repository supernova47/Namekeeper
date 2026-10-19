namespace SuperNova.ValheimNamekeeper
{
    internal static class RoofRequirements
    {
        internal static Piece.Requirement[] ForInstance(Piece.Requirement[] original, bool included)
        {
            var copy = new Piece.Requirement[original.Length];
            for (int i = 0; i < copy.Length; i++)
            {
                var r = original[i];
                copy[i] = new Piece.Requirement { m_resItem = r.m_resItem, m_amount = r.m_amount,
                    m_amountPerLevel = r.m_amountPerLevel, m_extraAmountOnlyOneIngredient = r.m_extraAmountOnlyOneIngredient,
                    m_upgraderResource = r.m_upgraderResource, m_recover = r.m_recover && RoofMigration.CanRefund(included) };
            }
            return copy;
        }
    }
}
