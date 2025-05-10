namespace ForecourtSimulator.Core;

public class TokheimPumpSimulator : PumpSimulator
{
    enum Command
    {
        GetPumpStatus = 0xA2,
        Authorize = 0xA5,
        Pause = 0xA3,
        Resume = 0xA4,
        SetPrice = 0xBF,
        GetFillingInfo = 0xA1,
        GetTotalizer = 0xA9
    }

    const int ADDRESS_OFFSET1 = 0xEF;
    const int ADDRESS_OFFSET2 = 0xBF;
    const int rxTimeOut = 100;

    public TokheimPumpSimulator(ISerialPortInterface serialPort, IPumpStorage pumpStore, int nPumps) : base(serialPort, pumpStore, nPumps)
    {
    }

    bool Receive(int dataExpected, out int dataReceived)
    {
        int d = 0;
        dataReceived = 0;
        if (!SerialPort.Read(out d, rxTimeOut))
            return false;
        dataReceived = d;
        if (dataExpected != -1 && d != dataExpected)
            return false;
        return true;
    }

    void WriteSingle(int data)
    {
        SerialPort.Write(data);
        SerialPort.Write(~data);
    }

    protected override void Write(int value)
    {
        WriteSingle(value);
    }
    bool ReadSingle(out int mdata)
    {
        mdata = 0;
        int data = 0;
        if (Receive(-1, out data))
        {
            int data2 = 0;
            if (Receive(-1, out data2))
            {
                if ((data & 0xFF) != (~data2 & 0xFF))
                {
                    return false;
                }
                mdata = data;
                return true;
            }
        }
        return false;
    }

    protected override int Receive()
    {
        int v = 0;
        if (ReadSingle(out v))
            return v;
        return -1;
    }

    void WriteStatus(int address)
    {
        int stat = GetPumpStatus(address) switch
        {
            PumpStatus.Idle => 0x2f,
            PumpStatus.NozzleDown => 0x20,
            PumpStatus.NozzleUp => 0xa0,
            PumpStatus.NozzleUpAuthorized => 0xd0,
            PumpStatus.Filling => 0xf0,
            PumpStatus.FilledLimit => 0x98,
            _ => 0
        };
        WriteSingle(stat);
        var pump = Pumps.SingleOrDefault(p => p.Address == address);
        if (pump != null)
        {
            pump.ProbeCount++;
            pump.StateChanged();
        }
    }

    protected override void RunLoop()
    {
        try
        {
            int header = 0, command = 0;
            while (ReadSingle(out header))
            {
                if (header > ADDRESS_OFFSET1 || header > ADDRESS_OFFSET2)
                {
                    var address = header > ADDRESS_OFFSET1 ? header - ADDRESS_OFFSET1 : header - ADDRESS_OFFSET2;
                    if (ReadSingle(out command))
                    {
                        if (!(address > ADDRESS_OFFSET1) && command == 0xA3)
                        {
                            command = (int)Command.SetPrice;
                        }
                        switch ((Command)command)
                        {
                            case Command.GetPumpStatus:
                                {
                                    WriteStatus(address);
                                    SerialPort.Flush();
                                }
                                break;
                            case Command.Authorize:
                                {
                                    int vv = 0;
                                    ReadSingle(out vv);
                                    int price = ReceiveBCD(2, true);
                                    var amount = ReceiveBCD(3, true);
                                    var volume = ReceiveBCD(3, true);
                                    if (amount == 0)
                                        StartSaleByVolume(address, price / Math.Pow(10, RateDP), volume / Math.Pow(10, VolumeDP));
                                    else
                                        StartSaleByAmount(address, price / Math.Pow(10, RateDP), amount / Math.Pow(10, AmountDP));
                                    WriteStatus(address);
                                    SerialPort.Flush();
                                }
                                break;
                            case Command.Pause:
                                {
                                    PauseSale(address);
                                    WriteStatus(address);
                                    SerialPort.Flush();
                                }
                                break;
                            case Command.Resume:
                                {
                                    ResumeSale(address);
                                    WriteStatus(address);
                                    SerialPort.Flush();
                                }
                                break;
                            case Command.SetPrice:
                                {
                                    int price1 = ReceiveBCD(4, false);
                                    int price2 = ReceiveBCD(4, false);
                                    int price3 = ReceiveBCD(4, false);
                                    int price4 = ReceiveBCD(4, false);
                                    int price5 = ReceiveBCD(4, false);
                                    SetPrice(address, (price1 / 10000) / Math.Pow(10, RateDP));
                                    WriteStatus(address);
                                    SerialPort.Flush();
                                }
                                break;
                            case Command.GetFillingInfo:
                                {
                                    WriteBCD((int)(GetPrice(address) * Math.Pow(10, RateDP)), 2, true);
                                    WriteBCD((int)(GetAmountSold(address) * Math.Pow(10, AmountDP)), 3, true);
                                    WriteBCD((int)(GetVolumeSold(address) * Math.Pow(10, VolumeDP)), 4, true);
                                    WriteStatus(address);
                                    SerialPort.Flush();
                                }
                                break;
                            case Command.GetTotalizer:
                                {
                                    int type = 0;
                                    ReadSingle(out type);
                                    WriteBCD((int)(GetTotalVolume(address) * Math.Pow(10, TotalVolumeDP)), 5, true);
                                    WriteBCD((int)(GetTotalAmount(address) * Math.Pow(10, TotalAmountDP)), 5, true);
                                    WriteStatus(address);
                                    SerialPort.Flush();
                                }
                                break;
                            default:
                                SerialPort.DiscardBuffered();
                                break;
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            //SerialPort.DiscardBuffered();
        }
    }
}
