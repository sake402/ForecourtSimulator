namespace ForecourtSimulator.Core;

public interface ISerialPortInterface
{
    void Write(int value);
    bool Read(out int value, int timeOut);
    void DiscardBuffered();
    void Flush();
}
