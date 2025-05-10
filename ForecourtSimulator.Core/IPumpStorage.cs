namespace ForecourtSimulator.Core;
public interface IPumpStorage
{
    Task Store(int address, double price, double volume, double amount);
    Task<PumpState> Load(int address);
}
