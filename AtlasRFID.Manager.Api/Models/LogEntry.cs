namespace AtlasRFID.Manager.Api.Models
{
    public class LogEntry
    {
        public Guid Id { get; set; }
        public Guid? CompanyId { get; set; }
        public string Category { get; set; }
        public string Level { get; set; }
        public string Source { get; set; }
        public string Message { get; set; }
        public string Details { get; set; }
        public string CorrelationId { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
    }
}
