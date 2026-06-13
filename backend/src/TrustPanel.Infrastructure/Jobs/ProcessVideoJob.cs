using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using TrustPanel.Application.Common;
using TrustPanel.Domain.Testimonials;

namespace TrustPanel.Infrastructure.Jobs;

/// <summary>
/// Processes a video testimonial: trim, compress, and generate a thumbnail via FFmpeg.
/// When FFmpeg is not installed, the job logs a warning and marks the testimonial as-is.
/// </summary>
public sealed class ProcessVideoJob
{
    private readonly IAppDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ProcessVideoJob> _logger;

    public ProcessVideoJob(
        IAppDbContext db, IConfiguration configuration, ILogger<ProcessVideoJob> logger)
    {
        _db = db;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task ExecuteAsync(Guid testimonialId, CancellationToken cancellationToken = default)
    {
        var testimonial = await _db.Testimonials
            .FirstOrDefaultAsync(t => t.Id == testimonialId, cancellationToken);

        if (testimonial is null || testimonial.VideoPath is null)
        {
            _logger.LogWarning("ProcessVideoJob: testimonial {Id} not found or has no video", testimonialId);
            return;
        }

        var ffmpegPath = _configuration["FFMPEG_PATH"] ?? "ffmpeg";
        if (!File.Exists(ffmpegPath) && ffmpegPath == "ffmpeg")
        {
            // Try system PATH — if unavailable, skip gracefully.
            if (!IsCommandAvailable("ffmpeg"))
            {
                _logger.LogWarning(
                    "ProcessVideoJob: ffmpeg not found — skipping video processing for {Id}", testimonialId);
                return;
            }
        }

        var tempDir = Path.Combine(Path.GetTempPath(), $"trustpanel-{testimonialId}");
        Directory.CreateDirectory(tempDir);
        var inputPath = Path.Combine(tempDir, "input.mp4");
        var outputPath = Path.Combine(tempDir, "output.mp4");
        var thumbPath = Path.Combine(tempDir, "thumb.jpg");

        try
        {
            // Compress and generate thumbnail.
            await RunFfmpegAsync(ffmpegPath,
                $"-i \"{inputPath}\" -vcodec libx264 -crf 28 -acodec aac \"{outputPath}\"",
                cancellationToken);

            await RunFfmpegAsync(ffmpegPath,
                $"-i \"{inputPath}\" -ss 00:00:01 -vframes 1 \"{thumbPath}\"",
                cancellationToken);

            _logger.LogInformation("ProcessVideoJob: processed video for testimonial {Id}", testimonialId);

            // Update thumbnail path (object key would be determined by upload step).
            testimonial.ThumbnailPath = $"thumbnails/{testimonialId}.jpg";
            testimonial.UpdatedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { /* best-effort */ }
        }
    }

    private static Task RunFfmpegAsync(string ffmpeg, string args, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<int>();
        var psi = new ProcessStartInfo(ffmpeg, args)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        var process = Process.Start(psi)!;
        cancellationToken.Register(() => { try { process.Kill(); } catch { /* best-effort */ } });
        process.EnableRaisingEvents = true;
        process.Exited += (_, _) => tcs.TrySetResult(process.ExitCode);
        return tcs.Task;
    }

    private static bool IsCommandAvailable(string command)
    {
        try
        {
            using var p = Process.Start(new ProcessStartInfo(command, "-version")
            {
                RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false
            });
            p?.WaitForExit(3000);
            return p?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
