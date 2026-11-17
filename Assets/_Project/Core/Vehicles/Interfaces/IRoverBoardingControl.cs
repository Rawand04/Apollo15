using SpaceSeries.Core.Player;

namespace SpaceSeries.Core.Vehicles
{
    public interface IRoverBoardingControl
    {
        bool IsControlLocked { get; }
        bool TryAcquireRoverControl(AstronautController astronaut);
        void ReleaseRoverControl();
        void ParkRoverControl();
        void RestoreOnFootControl();
    }
    public interface IRoverPassengerSeat
    {
        bool IsRiding { get; }
        AstronautController Rider { get; }
        bool TryBoardAstronaut(AstronautController astronaut);
        bool TryDismount();
    }
}
