namespace ForecourtSimulator.Core;

public interface ITankStorage
{
    Task Store(int address, double productHeight, double waterHeight, double temperature);
    Task<TankState> Load(int address);
}
