using FileService.Api.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace FileService.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<FileMetadata> Files => Set<FileMetadata>();
}