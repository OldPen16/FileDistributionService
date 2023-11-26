namespace FileDistributionService
{
    public class FileModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public string Type { get; set; }
        public long Size { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastDownloadedAt { get; set; } = new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        public int TotalDownloads { get; set; }

    }
}
