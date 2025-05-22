using ForecourtSimulator.Core;
using LivingThing.Core.Frameworks.Client.Interface;
using LivingThing.Core.Frameworks.Common.Logging;

namespace ForecourtSimulator.Services
{
    public class SimulatorState
    {
        public string? Port { get; set; }
        public int BaudRate { get; set; } = 9600;
        public string? Protocol { get; set; } = "TOKHEIM";
        public Dictionary<int, PumpState>? PumpStates { get; set; }
        public Dictionary<int, TankState>? TankStates { get; set; }
    }
    public class SimulatorWorkBenchService : IPumpStorage, ITankStorage, IDisposable
    {
        public Dictionary<string, PumpSimulator> PumpSimulators { get; }
        public SimulatorWorkBenchService(IClientUnitOfWork unitOfWork)
        {
            UnitOfWork = unitOfWork;
            Port = new IOPort(this);
            PumpSimulators = new Dictionary<string, PumpSimulator>
            {
                ["GO"] = new GOPumpSimulator(Port.PumpPort, this),
                ["TOKHEIM"] = new TokheimPumpSimulator(Port.PumpPort, this, 2)
            };
            TankSimulator = new StartItalianaATGSimulator(Port.TankPort, this, 2);
        }

        IClientUnitOfWork UnitOfWork { get; }
        public IOPort Port { get; }
        public PumpSimulator PumpSimulator => PumpSimulators.GetValueOrDefault(State.Protocol ?? "") ?? PumpSimulators["TOKHEIM"];
        public TankATGSimulator TankSimulator { get; }
        public SimulatorState State { get; private set; } = new SimulatorState();

        public async Task SaveState()
        {
            await UnitOfWork.Storage.SetItem("State", State);
        }
        Task<PumpState> IPumpStorage.Load(int address)
        {
            var state = State.PumpStates?.GetValueOrDefault(address) ?? default!;
            if (state.Price == 0)
                state.Price = 402;
            return Task.FromResult(state);
        }

        async Task IPumpStorage.Store(int address, double price, double volume, double amount)
        {
            var ps = new PumpState
            {
                Price = price,
                Volume = volume,
                Amount = amount
            };
            State.PumpStates ??= new();
            State.PumpStates[address] = ps;
            await SaveState();
        }

        public async Task ResetPumpStore()
        {
            foreach (var pump in PumpSimulator.Pumps)
            {
                pump.TotalAmountSold = 0;
                pump.TotalVolumeSold = 0;
            }
            State.PumpStates?.Clear();
            await SaveState();
        }

        Task<TankState> ITankStorage.Load(int address)
        {
            var state = State.TankStates?.GetValueOrDefault(address) ?? default!;
            return Task.FromResult(state);
        }

        async Task ITankStorage.Store(int address, double productHeight, double waterHeight, double temperature)
        {
            var ts = new TankState
            {
                ProductHeight = productHeight,
                WaterHeight = waterHeight,
                Temperature = temperature
            };
            State.TankStates ??= new();
            State.TankStates[address] = ts;
            await SaveState();
        }

        public async Task ResetTankStore()
        {
            foreach (var tank in TankSimulator.Tanks)
            {
                tank.ProductHeight = 0;
                tank.WaterHeight = 0;
                tank.Temperature = 0;
            }
            State.TankStates?.Clear();
            await SaveState();
        }

        CancellationTokenSource? cts;
        bool inited;
        public async Task Initialize()
        {
            if (!inited)
            {
                inited = true;
                var state = await UnitOfWork.Storage.GetItem<SimulatorState>("State") ?? State;
                State = state;
                foreach (var p in PumpSimulators)
                    await p.Value.Initialize();
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

        public void Dispose()
        {
            Port.Dispose();
        }
    }
}
