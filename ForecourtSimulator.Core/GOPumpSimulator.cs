namespace ForecourtSimulator.Core;
public enum GoPumpMode
{
    DirectPulser,
    SerialPulser
}
public class GOPumpSimulator : PumpSimulator
{
    public GOPumpSimulator(ISerialPortInterface serialPort, IPumpStorage pumpStore, GoPumpMode mode) : base(serialPort, pumpStore, mode == GoPumpMode.DirectPulser ? 1 : 2)
    {
        this.mode = mode;
        if (mode == GoPumpMode.DirectPulser)
        {
            Pumps[0].OnStateChanged += GOPumpSimulator_OnStateChanged;
        }
    }

    GoPumpMode mode;
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
                        2 => 1 | 4,
                        3 => 1 | 4 | 16,
                        4 => 1 | 4 | 16 | 64,
                        _ => 0
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
        if (mode == GoPumpMode.SerialPulser)
        {
            int pulse1 = (int)((Pumps[0].TotalVolumeSold + Pumps[0].CurrentVolumeSold) * PulsePerLiter);
            int pulse2 = (int)((Pumps[1].TotalVolumeSold + Pumps[1].CurrentVolumeSold) * PulsePerLiter);
            List<byte> packet = new List<byte>()
            {
                0x55,
                0xAA,
                (byte)((pulse1>>0) & 0xFF),
                (byte)((pulse1>>8) & 0xFF),
                (byte)((pulse1>>16) & 0xFF),
                (byte)((pulse1>>24) & 0xFF),
                (byte)((pulse2>>0) & 0xFF),
                (byte)((pulse2>>8) & 0xFF),
                (byte)((pulse2>>16) & 0xFF),
                (byte)((pulse2>>24) & 0xFF),
            };
            int csum = 0;
            for (int i = 2; i < packet.Count; i++)
            {
                csum += packet[i];
            }
            packet.Add((byte)(~csum + 1));
            SerialPort.Write(packet.ToArray());
            SerialPort.Flush();
        }
    }
    public int PulsePerLiter { get; set; } = 100;
    public double PricePerLiter { get; set; } = 402;
    public void BeginSaleByVolume(double volume)
    {
        StartSaleByVolume(1, PricePerLiter, volume);
    }
}
