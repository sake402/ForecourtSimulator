namespace ForecourtSimulator.Core;

public class Pump
{
    public PumpSimulator Simulator { get; }
    public int Address { get; }
    public Pump(PumpSimulator simulator, int address)
    {
        Simulator = simulator;
        Address = address;
    }

    public int ProbeCount { get; set; }
    public bool Online { get; set; }
    public bool Enable { get; set; } = true;
    public bool PauseSale { get; set; }
    public PumpStatus Status { get; set; }
    public double PresetPrice { get; set; }
    public double PresetVolume { get; set; }
    public double PresetAmount { get; set; }
    public double VolumeSold { get; set; }
    public double AmountSold { get; set; }
    public double TotalVolumeSold { get; set; }
    public double TotalAmountSold { get; set; }
    public Tank? ConnectedTank { get; set; }
    public bool Selling { get; private set; }

    public void ToggleFlow()
    {
        PauseSale = !PauseSale;
    }

    public void ToggleNozzle()
    {
        if (Status == PumpStatus.Idle || Status == PumpStatus.NozzleDown)
            Status = PumpStatus.NozzleUp;
        else if (Selling)
            Status = PumpStatus.FilledLimit;
        else
            Status = PumpStatus.NozzleDown;
    }

    async Task EndSale()
    {
        var totalVolumeBefore = TotalVolumeSold;
        var totalAmountBefore = TotalAmountSold;
        TotalVolumeSold += VolumeSold;
        TotalAmountSold += AmountSold;
        (double originalVolume, double newVolume) tankVolume = default;
        if (ConnectedTank != null)
            tankVolume = await ConnectedTank.DrawVolume(VolumeSold);
        Status = PumpStatus.FilledLimit;
        Selling = false;
        PresetAmount = 0;
        PresetVolume = 0;
        await Simulator.PumpStore.Store(Address, PresetPrice, TotalVolumeSold, TotalAmountSold);
        var log = $"{DateTime.Now} | {VolumeSold,6:#,##0.##}L | ₦{AmountSold,-10:#,##0.##} | ₦{PresetPrice,-6:#,##0.##} | {totalVolumeBefore,10:#,##0.##}L => {TotalVolumeSold,10:#,##0.##}L | ₦{totalAmountBefore,-15:#,##0.##} => ₦{TotalAmountSold,-15:#,##0.##}{(ConnectedTank != null ? $" | T{ConnectedTank.Address}: {tankVolume.originalVolume,8:#,##0.##}L => {tankVolume.newVolume,8:#,##0.##}L" : "")}\r\n";
        var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"salesLog{Address}.txt");
        if (!File.Exists(logPath))
        {
            File.Create(logPath);
        }
        File.AppendAllText(logPath, log);
    }

    public async Task Run()
    {
        if (!Online && Status == PumpStatus.NozzleUp)
        {
            Status = PumpStatus.NozzleUpAuthorized;
            StateChanged();
            await Task.Delay(500);
        }
        else if (Status == PumpStatus.NozzleUpAuthorized)
        {
            PauseSale = false;
            VolumeSold = 0;
            AmountSold = 0;
            Status = PumpStatus.Filling;
            StateChanged();
            await Task.Delay(500);
        }
        else if (Status == PumpStatus.Filling)
        {
            if (!PauseSale)
            {
                Selling = true;
                VolumeSold += 0.1;
                AmountSold = VolumeSold * PresetPrice;
                if (PresetVolume > 0 && VolumeSold >= PresetVolume - 0.01)
                {
                    VolumeSold = PresetVolume;
                    AmountSold = VolumeSold * PresetPrice;
                    await EndSale();
                }
                if (PresetAmount > 0 && AmountSold >= PresetAmount - 0.01)
                {
                    AmountSold = PresetAmount;
                    VolumeSold = AmountSold / PresetPrice;
                    await EndSale();
                }
            }
            StateChanged();
        }
        else if (Status == PumpStatus.FilledLimit && Selling)
        {
            await EndSale();
            Status = PumpStatus.NozzleDown;
            StateChanged();
        }
    }

    public event EventHandler? OnStateChanged;
    public void StateChanged()
    {
        OnStateChanged?.Invoke(this, EventArgs.Empty);
    }
}
