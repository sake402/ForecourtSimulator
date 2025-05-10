namespace ForecourtSimulator.Core;

public class TankDefaultCalibrationTable
{
    const int DataPoints = 1000000;

    static double EstimateVolume(double height, double tankHeight, double tankVolume, TankShapeTypes shape)
    {
        if (shape == TankShapeTypes.HorizontalCylinder)
        {
            double tankRadius = tankHeight / 2;
            double triangleHeight;
            if (height > tankHeight / 2)
            {
                triangleHeight = height - tankRadius;
            }
            else
            {
                triangleHeight = tankRadius - height;
            }
            double angle = 2 * Math.Acos(triangleHeight / tankRadius);
            double sectorArea = (Math.PI * tankRadius * tankRadius * angle) / (2 * Math.PI);
            double traingleArea = tankRadius * tankRadius * Math.Sin(angle) / 2;
            double segmentArea = sectorArea - traingleArea;
            double circleArea = Math.PI * tankRadius * tankRadius;
            double tankLenght = tankVolume / circleArea;
            double area;
            if (height > tankHeight / 2)
            {
                area = circleArea - segmentArea;
            }
            else
            {
                area = segmentArea;
            }
            return area * tankLenght;
        }
        return (height * tankVolume) / tankHeight;
    }

    static (double Height, double Volume) CalibrationTable(int i, double tankHeight, double tankVolume, TankShapeTypes shape)
    {
        double step = tankHeight / DataPoints;
        double h = i * step;
        double v = EstimateVolume(h, tankHeight, tankVolume, shape);
        return (h, v);
    }

    public static double LinearInterpolateVolume(double height, double tankHeight, double tankVolume, TankShapeTypes shape)
    {
        (double Height, double Volume) left = default;
        (double Height, double Volume) right = default;
        for (int i = 0; i < DataPoints; i++)
        {
            (double Height, double Volume) row = CalibrationTable(i, tankHeight, tankVolume, shape);
            if (row.Height > height)
            {
                right = row;
                break;
            }
            left = row;
        }
        double slope = (right.Volume - left.Volume) / (right.Height - left.Height);
        double intercept = right.Volume - slope * right.Height;
        return slope * height + intercept;
    }

    public static double LinearInterpolateHeight(double volume, double tankHeight, double tankVolume, TankShapeTypes shape)
    {
        (double Height, double Volume) left = default;
        (double Height, double Volume) right = default;
        for (int i = 0; i < DataPoints; i++)
        {
            (double Height, double Volume) row = CalibrationTable(i, tankHeight, tankVolume, shape);
            if (row.Volume > volume)
            {
                right = row;
                break;
            }
            left = row;
        }
        double slope = (right.Height - left.Height) / (right.Volume - left.Volume);
        double intercept = right.Height - slope * right.Volume;
        return slope * volume + intercept;
    }
}
