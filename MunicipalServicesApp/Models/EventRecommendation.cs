namespace MunicipalServicesApp.Models
{
    /// <summary>
    /// A recommended event together with its score and a human-readable reason.
    /// </summary>
    public class EventRecommendation
    {
        public LocalEvent Event { get; private set; }
        public int Score { get; private set; }
        public string Reason { get; private set; }

        public EventRecommendation(LocalEvent evt, int score, string reason)
        {
            Event = evt;
            Score = score;
            Reason = reason;
        }

        public override string ToString()
        {
            return string.Format("{0:dd MMM} - {1}  [{2}]", Event.Date, Event.Title, Reason);
        }
    }
}