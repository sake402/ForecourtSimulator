namespace ForecourtSimulator.Core;

public abstract class PumpSimulator
{
    public ISerialPortInterface SerialPort { get; }
    public IPumpStorage PumpStore { get; }
    public Pump[] Pumps { get; }
    public bool[] AutoSell { get; }
    int[] pumpToggleNozzleTime;
    protected PumpSimulator(ISerialPortInterface serialPort, IPumpStorage pumpStore, int nPumps)
    {
        SerialPort = serialPort;
        PumpStore = pumpStore;
        Pumps = new Pump[nPumps];
        AutoSell = new bool[nPumps];
        pumpToggleNozzleTime = new int[nPumps];
        for (int i = 0; i < nPumps; i++)
        {
            Pumps[i] = new Pump(this, i + 1);
        }
    }

    public async Task Initialize()
    {
        foreach (var pump in Pumps)
        {
            var tot = await PumpStore.Load(pump.Address);
            pump.PresetPrice = tot.Price;
            pump.TotalVolumeSold = tot.Volume;
            pump.TotalAmountSold = tot.Amount;
        }
    }
    public PumpStatus GetPumpStatus(int address)
    {
        var pump = Pumps.SingleOrDefault(p => p.Address == address);
        if (pump != null)
        {
            return pump.Status;
        }
        return default;
    }

    protected void SetPrice(int address, double price)
    {
        var pump = Pumps.SingleOrDefault(p => p.Address == address);
        if (pump != null)
        {
            pump.PresetPrice = price;
            pump.StateChanged();
        }
    }

    protected double GetPrice(int address)
    {
        var pump = Pumps.SingleOrDefault(p => p.Address == address);
        return pump?.PresetPrice ?? 0;
    }

    protected void StartSaleByVolume(int address, double price, double volume)
    {
        var pump = Pumps.SingleOrDefault(p => p.Address == address);
        if (pump?.Status == PumpStatus.NozzleUp)
        {
            pump.PresetPrice = price;
            pump.PresetVolume = volume;
            pump.PresetAmount = 0;
            pump.Status = PumpStatus.NozzleUpAuthorized;
            pump.StateChanged();
        }
    }
    protected void StartSaleByAmount(int address, double price, double amount)
    {
        var pump = Pumps.SingleOrDefault(p => p.Address == address);
        if (pump?.Status == PumpStatus.NozzleUp)
        {
            pump.PresetPrice = price;
            pump.PresetVolume = 0;
            pump.PresetAmount = amount;
            pump.Status = PumpStatus.NozzleUpAuthorized;
            pump.StateChanged();
        }
    }
    protected void PauseSale(int address)
    {
        var pump = Pumps.SingleOrDefault(p => p.Address == address);
        if (pump != null)
        {
            pump.PauseSale = true;
            pump.StateChanged();
        }
    }
    protected void ResumeSale(int address)
    {
        var pump = Pumps.SingleOrDefault(p => p.Address == address);
        if (pump != null)
        {
            pump.PauseSale = false;
            pump.StateChanged();
        }
    }

    public double GetVolumeSold(int address)
    {
        var pump = Pumps.SingleOrDefault(p => p.Address == address);
        return pump?.VolumeSold ?? 0;
    }

    public double GetAmountSold(int address)
    {
        var pump = Pumps.SingleOrDefault(p => p.Address == address);
        return pump?.AmountSold ?? 0;
    }

    public double GetTotalVolume(int address)
    {
        var pump = Pumps.SingleOrDefault(p => p.Address == address);
        return pump?.TotalVolumeSold ?? 0;
    }

    public double GetTotalAmount(int address)
    {
        var pump = Pumps.SingleOrDefault(p => p.Address == address);
        return pump?.TotalAmountSold ?? 0;
    }

    public int RateDP => 1;
    public int VolumeDP => 2;
    public int AmountDP => 0;
    public int TotalVolumeDP => 2;
    public int TotalAmountDP => 2;
    protected abstract int Receive();
    protected abstract void Write(int value);
    protected void WriteBCD(int number, int size, bool lowToHigh, bool compliment = false)
    {
        int sz = size;
        int max = 1;
        while (sz-- > 0)
            max *= 100;
        number %= max;
        max /= 10;
        for (int i = 0; i < size; i++)
        {
            if (!lowToHigh)
            {
                int t = number / max;
                number = number - t * max;
                max /= 10;
                int u = number / max;
                number = number - u * max;
                max /= 10;
                int bcd = (t << 4) | u;
                if (compliment)
                    bcd ^= 0xFF;
                Write(bcd);
            }
            else
            {
                int u = number % 10;
                number /= 10;
                int t = number % 10;
                number /= 10;
                int bcd = (t << 4) | u;
                if (compliment)
                    bcd ^= 0xFF;
                Write(bcd);
            }
        }
    }

    protected int ReceiveBCD(int count, bool lowToHigh)
    {
        int sz = count - 1;
        int multiplier = 1;
        int v = 0;
        if (!lowToHigh)
        {
            while (sz-- > 0)
                multiplier *= 100;
        }
        for (int i = 0; i < count; i++)
        {
            int a = Receive();
            if (a < 0)
                return a;
            int t = a >> 4;
            int u = a & 0x0F;
            a = 10 * t + u;
            v += a * multiplier;
            if (!lowToHigh)
                multiplier /= 100;
            else
                multiplier *= 100;
        }
        return v;
    }

    protected abstract void RunLoop();

    DateTime lastRun;
    public async Task Run()
    {
        RunLoop();
        if (lastRun != default)
        {
            var elapsedMs = (int)(DateTime.UtcNow - lastRun).TotalMilliseconds;
            for (int i = 0; i < pumpToggleNozzleTime.Length; i++)
            {
                if (AutoSell[i])
                {
                    pumpToggleNozzleTime[i] -= elapsedMs;
                    if (pumpToggleNozzleTime[i] <= 0)
                    {
                        Pumps[i].ToggleNozzle();
                        Pumps[i].StateChanged();
                        if (Pumps[i].Status != PumpStatus.NozzleUp)
                        {
                            pumpToggleNozzleTime[i] = 2000; //keep nozzle down for 2s
                        }
                        else
                        {
                            pumpToggleNozzleTime[i] = Random.Shared.Next(2000, 20000); //sell for a random period of time
                        }
                    }
                }
            }
        }
        foreach (var pump in Pumps)
            await pump.Run();
        lastRun = DateTime.UtcNow;
    }
}
