using QLNet;
namespace R2QLNet;

public class CBInterpolation: Interpolation
{
    public CBInterpolation(
        List<double> xBegin, int xEnd,
        List<double> yBegin, int yN,
        Behavior behavior,
        CubicInterpolation.DerivativeApprox da,
        bool monotonic,
        CubicInterpolation.BoundaryCondition leftC,
        double leftConditionValue,
        CubicInterpolation.BoundaryCondition rightC,
        double rightConditionValue)
    {
        impl_ = new MixedInterpolationImpl<BackwardFlat, Cubic>(xBegin, xEnd, yBegin, yN, behavior,
                                                          new BackwardFlat(),
                                                          new Cubic(da, monotonic, leftC, leftConditionValue, rightC, rightConditionValue));
        impl_.update();
    }
}

public class CBInterpFactory : IInterpolationFactory
{
    private int meetingCount_;

    public CBInterpFactory(): this(0) { }

    public CBInterpFactory(int meetingCount)
    {
        meetingCount_ = meetingCount;
    }

    public bool global => true;

    public int requiredPoints => 2;

    public Interpolation interpolate(List<double> xBegin, int size, List<double> yBegin)
    {
        return new CBInterpolation(xBegin, size, yBegin, meetingCount_ + 1,
            Behavior.SplitRanges, CubicInterpolation.DerivativeApprox.Spline,
            false, CubicInterpolation.BoundaryCondition.FirstDerivative, 0.0, CubicInterpolation.BoundaryCondition.FirstDerivative, 0.0);
    }
}
