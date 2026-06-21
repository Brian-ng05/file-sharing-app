using FileService.Api.Entities;

namespace FileService.Api.Repository
{
    public interface IFileRepository
    {
        Task AddAsync(FileMetadata file);

        Task<FileMetadata?> GetByCodeAsync(string code);

        Task DeleteAsync(FileMetadata file);

        Task SaveChangesAsync();
    }
}
