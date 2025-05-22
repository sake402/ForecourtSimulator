namespace ForecourtSimulator.Core;

public class GOPumpSimulator : PumpSimulator
{
    public GOPumpSimulator(ISerialPortInterface serialPort, IPumpStorage pumpStore) : base(serialPort, pumpStore, 1)
    {
        Pumps[0].OnStateChanged += GOPumpSimulator_OnStateChanged;
    }

    double lastVolume;
    private void GOPumpSimulator_OnStateChanged(object? sender, EventArgs e)
    {
        if (Pumps[0].VolumeSold != lastVolume)
        {
            if (Pumps[0].VolumeSold > lastVolume)
            {
                int pulse = (int)Math.Round((Pumps[0].VolumeSold - lastVolume) * PulsePerLiter);
                List<byte> pulses = new List<byte>();
                while (pulse >= 5)
                {
                    pulses.Add(0x55);
                    pulse -= 5;
                }
                if (pulse > 0)
                {
                    byte lastByte = pulse switch
                    {
                        0 => 0,
                        1 => 1,
                        2 => 1|4,
                        3 => 1|4|16,
                        4 => 1|4|16|64,
                        _=> 0
                    };
                    pulses.Add(lastByte);
                }
                SerialPort.Write(pulses.ToArray());
                SerialPort.Flush();
            }
            lastVolume = Pumps[0].VolumeSold;
        }
    }

    protected override int Receive()
    {
        return 0;
    }
    protected override void Write(int value)
    {
    }

    protected override void RunLoop()
    {
    }
    public int PulsePerLiter { get; set; } = 100;
    public double PricePerLiter { get; set; } = 402;
    public void BeginSaleByVolume(double volume)
    {
        StartSaleByVolume(1, PricePerLiter, volume);
    }
}
