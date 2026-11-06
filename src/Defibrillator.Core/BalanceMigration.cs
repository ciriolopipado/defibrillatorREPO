namespace Defibrillator.Core;

/// <summary>One-time conversion of previous test defaults, preserving other custom values.</summary>
public static class BalanceMigration
{
    public const int Revision = 1;
    public const int Price = 2;
    public const int BatteryCost = 50;

    public static (int Price, int BatteryCost) Apply(int revision, int price, int batteryCost)
    {
        if (revision >= Revision) return (price, batteryCost);
        return (price == 0 ? Price : price, batteryCost == 25 ? BatteryCost : batteryCost);
    }
}
