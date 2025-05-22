using System.Diagnostics;

namespace ForecourtSimulator.Core;

public class Tank
{
    public TankATGSimulator Simulator { get; }
    public int Address { get; }
    public bool Enable { get; set; } = true;
    public double Height { get; }
    public (double, double)[] CaliberationTable { get; }
    public Tank(TankATGSimulator simulator, int address, double height)
    {
        Simulator = simulator;
        Address = address;
        Height = height;
        int count = 1000000;
        double step = height / count;
        (double Height, double Volume)[] table = new (double, double)[count];
        for (int i = 0; i < count; i++)
        {
            double h = (i + 1) * step;
            double v = EstimateVolume(h, height, 10000);
            table[i] = (h, v);
        }
        CaliberationTable = table;
    }

    double LinearInterpolateVolume(double height)
    {
        (double Height, double Volume) left = default;
        (double Height, double Volume) right = default;
        for (int i = 0; i < CaliberationTable.Length; i++)
        {
            (double Height, double Volume) row = CaliberationTable[i];
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

    double LinearInterpolateHeight(double volume)
    {
        (double Height, double Volume) left = default;
        (double Height, double Volume) right = default;
        for (int i = 0; i < CaliberationTable.Length; i++)
        {
            (double Height, double Volume) row = CaliberationTable[i];
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

    public async Task<(double OriginalVolume, double NewVolume)> DrawVolume(double volume)
    {
        var originalVolume = ProductVolume;
        var newVolume = ProductVolume - volume;
        if (newVolume < 0)
            newVolume = 0;
        var newHeight = LinearInterpolateHeight(newVolume);
        if (newHeight < 0)
            newHeight = 0;
        var newVolumeCheck = LinearInterpolateVolume(newHeight);
        var error = Math.Abs(newVolume - newVolumeCheck);
        Debug.Assert(error < 1);
        ProductHeight = newHeight;
        StateChanged();
        await Simulator.TankStore.Store(Address, ProductHeight, WaterHeight, Temperature);
        return (originalVolume, newVolumeCheck);
    }

    public int ProbeCount { get; set; }
    public double ProductHeight { get; set; }
    public double WaterHeight { get; set; }
    public double Temperature { get; set; }
    public int ProductHeightPercentage => (int)(ProductHeight * 100 / Height);
    public int WaterHeightPercentage => (int)(WaterHeight * 100 / Height);
    public double ProductVolume => LinearInterpolateVolume(ProductHeight);
    public double WaterVolume => LinearInterpolateVolume(WaterHeight);

    public event EventHandler? OnStateChanged;
    public void StateChanged()
    {
        OnStateChanged?.Invoke(this, EventArgs.Empty);
    }

    public override string ToString()
    {
        return $"Tank " + Address;
    }
    static double EstimateVolume(double height, double tankHeight, double tankVolume)
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

    //double EstimateHeight(double area, double tankHeight)
    //{
    //    double tankRadius = tankHeight / 2;
    //    double circleArea = Math.PI * tankRadius * tankRadius;
    //    double segmentArea;
    //    double triangleHeight;
    //    if (area < circleArea / 2)
    //    {
    //        triangleHeight = tankRadius - ;
    //        segmentArea = area;
    //    }
    //    else
    //    {
    //        segmentArea = circleArea - area;
    //    }
    //    double sectorArea = (Math.PI * tankRadius * tankRadius * angle) / (2 * Math.PI);
    //    if (productHeight > tankHeight / 2)
    //    {
    //        triangleHeight = productHeight - tankRadius;
    //    }
    //    else
    //    {
    //        triangleHeight = tankRadius - productHeight;
    //    }
    //    double angle = 2 * Math.Acos(triangleHeight / tankRadius);
    //    double traingleArea = tankRadius * tankRadius * Math.Sin(angle) / 2;
    //    double segmentArea = sectorArea - traingleArea;
    //    double circleArea = Math.PI * tankRadius * tankRadius;
    //    if (productHeight > tankHeight / 2)
    //    {
    //        return circleArea - segmentArea;
    //    }
    //    else
    //    {
    //        return segmentArea;
    //    }
    //}
}
