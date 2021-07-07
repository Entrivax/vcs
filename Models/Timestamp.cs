using VCS.Utils;

namespace VCS.Models
{
    public class Timestamp
    {
        public float Time { get; }

        public Timestamp(float time)
        {
            this.Time = time;
        }

        public override string ToString()
        {
            return TimestampUtils.SecondsToTimestamp(Time);
        }
    }
}
