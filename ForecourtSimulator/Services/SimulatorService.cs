using ForecourtSimulator.Core;
using LivingThing.Core.Frameworks.Client.Interface;
using LivingThing.Core.Frameworks.Common.Logging;

namespace ForecourtSimulator.Services
{
    public class SimulatorService : IPumpStorage, ITankStorage
    {
        public SimulatorService(IClientUnitOfWork unitOfWork, IOPort port)
        {
            UnitOfWork = unitOfWork;
            Port = port;
            PumpSimulator = new TokheimPumpSimulator(Port.PumpPort, this, 2);
            TankSimulator = new StartItalianaATGSimulator(Port.TankPort, this, 2);
        }

        IClientUnitOfWork UnitOfWork { get; }
        public IOPort Port { get; }
        public PumpSimulator PumpSimulator { get; }
        public TankATGSimulator TankSimulator { get; }

        async Task<PumpState> IPumpStorage.Load(int address)
        {
            var state = await UnitOfWork.Storage.GetItem<PumpState>("PS" + address);
            if (state.Price == 0)
                state.Price = 402;
            return state;
        }

        async Task IPumpStorage.Store(int address, double price, double volume, double amount)
        {
            await UnitOfWork.Storage.SetItem("PS" + address, new PumpState
            {
                Price = price,
                Volume = volume,
                Amount = amount
            });
        }

        public async Task ResetPumpStore()
        {
            foreach (var pump in PumpSimulator.Pumps)
            {
                pump.TotalAmountSold = 0;
                pump.TotalVolumeSold = 0;
                await UnitOfWork.Storage.SetItem("PS" + pump.Address, default(PumpState));
            }
        }

        async Task<TankState> ITankStorage.Load(int address)
        {
            var state = await UnitOfWork.Storage.GetItem<TankState>("TS" + address);
            return state;
        }

        async Task ITankStorage.Store(int address, double productHeight, double waterHeight, double temperature)
        {
            await UnitOfWork.Storage.SetItem("TS" + address, new TankState
            {
                ProductHeight = productHeight,
                WaterHeight = waterHeight,
                Temperature = temperature
            });
        }

        public async Task ResetTankStore()
        {
            foreach (var tank in TankSimulator.Tanks)
            {
                tank.ProductHeight = 0;
                tank.WaterHeight = 0;
                tank.Temperature = 0;
                await UnitOfWork.Storage.SetItem("TS" + tank.Address, default(TankState));
            }
        }

        CancellationTokenSource? cts;
        bool inited;
        public async Task Initialize()
        {
            if (!inited)
            {
                inited = true;
                await PumpSimulator.Initialize();
                await TankSimulator.Initialize();
                _ = Task.Run(async () =>
                {
                    cts = new CancellationTokenSource();
                    while (!cts.IsCancellationRequested)
                    {
                        await Task.Delay(100, cts.Token);
                        try
                        {
                            await PumpSimulator.Run();
                        }
                        catch (Exception e)
                        {
                            LoggingFactory.LogException(e);
                        }
                        try
                        {
                            await TankSimulator.Run();
                        }
                        catch (Exception e)
                        {
                            LoggingFactory.LogException(e);
                        }
                    }
                });
            }
        }

    }
}
