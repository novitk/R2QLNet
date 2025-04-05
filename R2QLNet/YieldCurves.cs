using QLNet;
using MoreLinq;
using QDate = QLNet.Date;

namespace R2QLNet;

public static class YieldCurves
{
    private record InterpolationContext(
        QDate Today, List<RateHelper> Helpers, DayCounter DayCounter, int MeetingCount);

    private static PiecewiseYieldCurve MakeCBCubic(InterpolationContext ctx)
    {

        return new PiecewiseYieldCurve<ForwardRate, CBInterpFactory>(
            ctx.Today, ctx.Helpers, ctx.DayCounter,
            [], [], 1.0e-12, new CBInterpFactory(ctx.MeetingCount));
    }

    private static PiecewiseYieldCurve MakeSimpleCurve<Traits, Interpolator>(InterpolationContext ctx)
        where Traits : ITraits<YieldTermStructure>, new()
        where Interpolator : IInterpolationFactory, new()
    {
        return new PiecewiseYieldCurve<Traits, Interpolator>(ctx.Today, ctx.Helpers, ctx.DayCounter);
    }

    private static Dictionary<string, Func<InterpolationContext, PiecewiseYieldCurve>> curveFactory_ = new() {
        { "ConstForward", MakeSimpleCurve<ForwardRate, BackwardFlat> },
        { "CBCubic", MakeCBCubic },
    };

    public static PiecewiseYieldCurve MakeSofr(
        string interpolation, QDate t0, double fixingQuote,
        (QDate d, double v)[] meetings,
        QDate lastMeeting,
        (Month month, int year, double v)[] immfutures)
    {
        var today = Lazies.UnitedStatesBonds.Value.adjust(QDate.Today);
        var sofr = new Sofr();

        SimpleQuote sofrFixingQuote = new SimpleQuote(fixingQuote);
        Handle<Quote> sofrFixingQuoteHandle = new Handle<Quote>(sofrFixingQuote);
        RateHelper fixing = new DepositRateHelper(sofrFixingQuoteHandle, ts => new Sofr(ts), meetings[0].d);

        List<RateHelper> meetingSwaps = [.. meetings.Pairwise((m1, m2) => {
            SimpleQuote quote = new SimpleQuote(m1.v);
            Handle<Quote> handle = new Handle<Quote>(quote);
            return new DatedOISRateHelper(m1.d, m2.d, handle, sofr);
        })];
        var lastMeetingSwapStart = meetings[meetings.Length - 1].d;
        var lastMeetingSwapRate = meetings[meetings.Length - 1].v;
        SimpleQuote quote = new SimpleQuote(lastMeetingSwapRate);
        Handle<Quote> handle = new Handle<Quote>(quote);
        meetingSwaps.Add(new DatedOISRateHelper(lastMeetingSwapStart, lastMeeting, handle, sofr));

        //TODO: hack

        List<RateHelper> futures = [.. immfutures.Select(f => {
            SimpleQuote quote = new SimpleQuote(f.v);
            Handle<Quote> handle = new Handle<Quote>(quote);
            return new SofrFutureRateHelper(handle, f.month, f.year, Frequency.Quarterly);
        })];

        if (curveFactory_.TryGetValue(interpolation, out var factory))
        {
            return factory(new InterpolationContext(
                today,
                meetingSwaps.Concat(futures).Prepend(fixing).ToList(), Lazies.Actual360.Value, meetingSwaps.Count + 1));
        }
        else
            throw new ArgumentException($"Unknown interpolation {interpolation}");
    }
}
