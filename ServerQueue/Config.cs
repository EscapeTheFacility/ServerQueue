using System.ComponentModel;

namespace ServerQueue
{
    public class Config
    {
        public bool IsEnabled { get; set; } = true;
        [Description("Time between rejoins of a queue position (in seconds).")]
        public float TimeBetweenJoins { get; set; } = 10f;
        [Description("Error margin (in seconds). A user's queue position will be held for a specific time after the rejoin signal has been sent. After this, the user's queue entry will be cancelled. WARNING: setting this to a low amount might cause users to be unable to connect.")]
        public float ErrorMargin { get; set; } = 10f;
    }
}