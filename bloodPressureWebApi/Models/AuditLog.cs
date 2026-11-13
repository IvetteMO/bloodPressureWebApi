namespace bloodPressureWebApi.Models
{
    public class AuditLog
    {
        public int AuditLogId { get; set; }

        public string Action { get; set; } = "";

        public string EntityName { get; set; } ="";

        public int EntityId { get; set; }

        public string? OldValues { get; set; }

        public string? NewValues { get; set; }

        public string? ModelVersion { get; set; }

        public DateTime CreatedAt { get; set; }
            = DateTime.Now;

        // ---------- Quién y cómo (agregado con el login) ----------

        public int? UserId { get; set; }

        public string? UserEmail { get; set; }

        public string? UserRole { get; set; }

        public string? HttpMethod { get; set; }

        public string? Path { get; set; }

        public int? StatusCode { get; set; }

        public string? IpAddress { get; set; }
    }
}
