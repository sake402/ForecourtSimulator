using ForecourtSimulator.Core;
using System.Diagnostics;
using System.IO.Ports;

namespace ForecourtSimulator.Services
{
    public class IOPort : ISerialPortInterface, IDisposable
    {
        SimulatorWorkBenchService _service;
        SerialPort? port;
        public SharedSerialPort PumpPort { get; }
        public SharedSerialPort TankPort { get; }

        public IOPort(SimulatorWorkBenchService service)
        {
            this._service = service;
            PortNames = SerialPort.GetPortNames();
            PortName = PortNames.FirstOrDefault();
            PumpPort = new SharedSerialPort(this);
            TankPort = new SharedSerialPort(this);
        }

        public IEnumerable<string> PortNames { get; private set; } = Enumerable.Empty<string>();
        public string? PortName
        {
            get => _service.State.Port;
            set => _service.State.Port = value;
        }
        public void Write(byte[] value)
        {
            if (port != null)
                port.Write(value, 0, value.Length);
        }

        public void Write(int value)
        {
            if (port != null)
                port.Write(new byte[] { (byte)value }, 0, 1);
        }

        public bool Read(out int value, int timeOut)
        {
            value = 0;
            return false;
            if (port != null)
            //if (port!.BaseStream.Length > 0)
            {
                port.ReadTimeout = timeOut;
                byte[] b = new byte[100];
                int l = port.Read(b, 0, b.Length);
                if (l > 0)
                {
                    value = b[0];
                    for (int i = 0; i < l; i++)
                    {
                        //Debug.WriteLine(b[i]);
                        PumpPort.Feed(b[i]);
                        TankPort.Feed(b[i]);
                    }
                    return true;
                }
            }
            value = -1;
            return false;
        }

        public void Flush()
        {
            if (port != null)
                port.BaseStream.Flush();
        }

        public void DiscardBuffered()
        {
            if (port != null)
                port.DiscardInBuffer();
        }

        public bool IsConnected => port?.IsOpen ?? false;
        public void ToggleConnect()
        {
            if (port?.IsOpen ?? false)
            {
                PortNames = SerialPort.GetPortNames();
                port.Close();
            }
            else if (PortName != null)
            {
                port?.Close();
                port = new SerialPort(PortName, _service.State.BaudRate, Parity.None, 8, StopBits.One);
                port.DataReceived += Port_DataReceived;
                port.Open();
            }
        }

        private void Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (e.EventType == SerialData.Chars)
            {
                byte[] b = new byte[100];
                int l = port!.Read(b, 0, b.Length);
                if (l > 0)
                {
                    for (int i = 0; i < l; i++)
                    {
                        //Debug.WriteLine(b[i]);
                        PumpPort.Feed(b[i]);
                        TankPort.Feed(b[i]);
                    }
                }
            }
        }

        public void Dispose()
        {
            port?.Close();
        }
    }
}
