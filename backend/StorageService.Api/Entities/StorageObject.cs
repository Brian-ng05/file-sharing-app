namespace StorageService.Api.Entities
{
    public class StorageObject
    {
        public Guid Id { get; set; }

        public string StorageKey { get; set; } = string.Empty;

        public string BucketName { get; set; } = string.Empty;

        public string ContentType { get; set; } = string.Empty;

        public long Size { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
