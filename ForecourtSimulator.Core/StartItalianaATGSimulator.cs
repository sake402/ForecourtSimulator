using System.Text;

namespace ForecourtSimulator.Core;

public class StartItalianaATGSimulator : TankATGSimulator
{
    public StartItalianaATGSimulator(ISerialPortInterface serialPort, ITankStorage tankStore, int nTanks) : base(serialPort, tankStore, nTanks)
    {
    }

    string ReadLine()
    {
        string s = "";
        do
        {
            if (SerialPort.Read(out int v, 1000))
            {
                if (v == '\r' || v == '\n')
                    break;
                s += (char)v;
            }
        } while (true);
        return s;
    }

    protected override void RunLoop()
    {
        try
        {
            int header;
            while (SerialPort.Read(out header, 0))
            {
                if (header == 'M')
                {
                    var add = ReadLine();
                    if (!string.IsNullOrEmpty(add))
                    {
                        var iadd = int.Parse(add);
                        var tank = Tanks.FirstOrDefault(t => t.Address == iadd);
                        if (tank != null && tank.Enable)
                        {
                            string write = $"{add}=0={(int)(tank.Temperature * 10)}={(tank.ProductHeight * 10):f}={tank.WaterHeight:f}=0\r\n";
                            var bytes = Encoding.ASCII.GetBytes(write).Select(b => (int)b);
                            foreach (var b in bytes)
                            {
                                SerialPort.Write(b);
                            }
                            SerialPort.Flush();
                            tank.ProbeCount++;
                            tank.StateChanged();
                        }
                    }
                }
            }
        }
        catch (Exception)
        {

        }
    }
}
