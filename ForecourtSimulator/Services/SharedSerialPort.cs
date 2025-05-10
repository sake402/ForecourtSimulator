using ForecourtSimulator.Core;

namespace ForecourtSimulator.Services
{
    public class SharedSerialPort : ISerialPortInterface
    {
        IOPort originalPort;

        public SharedSerialPort(IOPort originalPort)
        {
            this.originalPort = originalPort;
        }

        public void DiscardBuffered()
        {
            received.Clear();
        }

        public void Flush()
        {
            originalPort.Flush();
        }

        List<int> received = new List<int>();
        ManualResetEvent receiveHandle = new ManualResetEvent(false);

        internal void Feed(int value)
        {
            lock (received)
            {
                received.Add(value);
            }
            receiveHandle.Set();
        }

        public bool Read(out int value, int timeOut)
        {
            if (received.Count == 0)
            {
                receiveHandle.Reset();
                receiveHandle.WaitOne(timeOut);
                receiveHandle.Reset();
            }
            if (received.Count > 0)
            {
                lock (received)
                {
                    value = received[0];
                    received.RemoveAt(0);
                }
                return true;
            }
            if (originalPort.Read(out value, timeOut))
            {
                lock (received)
                {
                    received.RemoveAt(0);
                    return true;
                }
            }
            return false;
        }

        public void Write(int value)
        {
            originalPort.Write(value);
        }
    }
}
