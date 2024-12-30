using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Scanvirus.Models;

namespace Scanvirus.Data
{

    public class FileModel
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public byte[] FileData { get; set; }
        public DateTime UploadedAt { get; set; }
        public string UploadedBy { get; set; }
    }
    

    public class AppDbContext : IdentityDbContext<Users>
    {
        public AppDbContext(DbContextOptions options) : base(options)
        {
        }
        public DbSet<FileModel> Files { get; set; }
    }

}