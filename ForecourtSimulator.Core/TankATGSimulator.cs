namespace ForecourtSimulator.Core;

public abstract class TankATGSimulator
{
    public ISerialPortInterface SerialPort { get; }
    public ITankStorage TankStore { get; }
    public Tank[] Tanks { get; }
    protected TankATGSimulator(ISerialPortInterface serialPort, ITankStorage tankStore, int nTanks)
    {
        SerialPort = serialPort;
        TankStore = tankStore;
        Tanks = new Tank[nTanks];
        for (int i = 0; i < nTanks; i++)
        {
            Tanks[i] = new Tank(this, i + 1000 + 1, 1000);
        }
    }


    public async Task Initialize()
    {
        foreach (var tank in Tanks)
        {
            var state = await TankStore.Load(tank.Address);
            tank.ProductHeight = Math.Max(0, state.ProductHeight);
            tank.WaterHeight = Math.Max(0, state.WaterHeight);
            tank.Temperature = Math.Max(0, state.Temperature);
        }
    }

    protected abstract void RunLoop();

    public async Task Run()
    {
        RunLoop();
        //foreach (var pump in Pumps)
        //    await pump.Run();
    }
}
