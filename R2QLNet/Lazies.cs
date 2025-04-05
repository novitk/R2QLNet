using QLNet;

namespace R2QLNet;

public static class Lazies
{
    public static Lazy<Actual360> Actual360 => new Lazy<Actual360>(() => new Actual360());
    public static Lazy<Calendar> UnitedStatesBonds => new Lazy<Calendar>(() => new UnitedStates(UnitedStates.Market.GovernmentBond));
}
