/// <summary>
/// Segitiga elemen plus Volt (P4). Isinya sama dengan kolom strongAgainst di CSV.
/// Kuat x1,5, kebalikannya x0,75, lainnya x1. Dipakai rumus damage P5.
/// </summary>
public static class ElementChart
{
    public static Element StrongAgainst(Element attacker)
    {
        switch (attacker)
        {
            case Element.Pyro: return Element.Sprout;
            case Element.Aqua: return Element.Pyro;
            case Element.Sprout: return Element.Aqua;
            default: return Element.Aqua; // Volt
        }
    }

    public static float Multiplier(Element attacker, Element defender)
    {
        if (StrongAgainst(attacker) == defender) return 1.5f;
        if (StrongAgainst(defender) == attacker) return 0.75f;
        return 1f;
    }
}
