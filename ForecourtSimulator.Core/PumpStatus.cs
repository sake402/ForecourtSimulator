namespace ForecourtSimulator.Core;

public enum PumpStatus
{
    Idle,
    NozzleDown,
    NozzleUp,
    NozzleUpAuthorized,
    NozzleDownAuthorized,
    Filling,
    FilledLimit
}
