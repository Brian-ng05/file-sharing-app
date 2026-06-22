using FileService.Api.Data;
using FileService.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace FileService.Api.Repository
{
    public class FileRepository : IFileRepository
    {
        private readonly ApplicationDbContext _db;

        public FileRepository(ApplicationDbContext db)
        {
            _db = db;
            _db = db;
        }

        public async Task AddAsync(FileMetadata file)
        {
            await _db.Files.AddAsync(file);
        }

        public async Task<FileMetadata?> GetByCodeAsync(string code)
        {
            return await _db.Files.FirstOrDefaultAsync(x => x.Code == code);
        }

        public async Task DeleteAsync(FileMetadata file)
        {
            _db.Files.Remove(file);
            await Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _db.SaveChangesAsync();
        }
    }
}