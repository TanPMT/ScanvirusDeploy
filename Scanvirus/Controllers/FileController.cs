using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using Scanvirus.Data;

namespace Scanvirus.Controllers
{
    [Authorize]
    [Route("files")]
    public class FileController : Controller
    {
        private readonly AppDbContext _context;

        public FileController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var files = await _context.Files
                .Select(f => new { f.Id, f.FileName, f.ContentType, f.UploadedAt, f.UploadedBy })
                .ToListAsync();

            return View(files);
        }

        [HttpGet("upload")]
        public IActionResult Upload()
        {
            return View();
        }

        //private readonly string clamAVPath = Path.Combine(Directory.GetCurrentDirectory(), @"bin", @"clamav-1.0.7.win.x64", "clamscan.exe");
        private readonly string clamAVPath = "/usr/bin/clamscan";
        private bool ScanFileWithClamAV(string filePath)
        {
            ProcessStartInfo processInfo = new ProcessStartInfo
            {
                FileName = clamAVPath,
                Arguments = $"{filePath}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            Process process = new Process
            {
                StartInfo = processInfo
            };

            process.Start();
            process.WaitForExit();

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            return output.Contains("OK");
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                ViewBag.Message = "Vui lòng chọn file!";
                return View("Upload"); // Trả về view Upload
            }

            // Tạo đường dẫn tạm để lưu file
            var tempFilePath = Path.GetTempFileName();
            using (var stream = new FileStream(tempFilePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Scan file bằng ClamAV
            var isClean = ScanFileWithClamAV(tempFilePath);

            // Xóa file tạm sau khi scan
            System.IO.File.Delete(tempFilePath);

            if (!isClean)
            {
                ViewBag.Message = "File chứa virus! Vui lòng kiểm tra lại.";
                return RedirectToAction("Privacy", "Home"); // Trả về view Upload nếu file có virus
            }

            // Lưu file nếu không có virus
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);

            var fileModel = new FileModel
            {
                FileName = file.FileName,
                ContentType = file.ContentType,
                FileData = memoryStream.ToArray(),
                UploadedAt = DateTime.UtcNow,
                UploadedBy = User.Identity?.Name
            };

            _context.Files.Add(fileModel);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        [HttpGet("download/{id}")]
        public async Task<IActionResult> DownloadFile(int id)
        {
            var file = await _context.Files.FindAsync(id);
            if (file == null)
                return NotFound();

            return File(file.FileData, file.ContentType, file.FileName);
        }
    }
}
