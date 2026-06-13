using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TrustPanel.Application.Common;
using TrustPanel.Domain.Testimonials;

namespace TrustPanel.Infrastructure.Jobs;

/// <summary>
/// Hangfire job that processes a CSV import source.
/// CSV format: Name,Email,Content,Rating,Company,JobTitle
/// </summary>
public sealed class ImportTestimonialsCsvJob
{
    private readonly IAppDbContext _db;
    private readonly ILogger<ImportTestimonialsCsvJob> _logger;

    public ImportTestimonialsCsvJob(IAppDbContext db, ILogger<ImportTestimonialsCsvJob> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task ExecuteAsync(Guid importSourceId, string csvContent, CancellationToken cancellationToken = default)
    {
        var source = await _db.ImportSources
            .FirstOrDefaultAsync(s => s.Id == importSourceId, cancellationToken);
        if (source is null)
        {
            _logger.LogWarning("ImportSource {Id} not found", importSourceId);
            return;
        }

        var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var imported = 0;

        foreach (var line in lines.Skip(1)) // skip header
        {
            var cols = line.Split(',');
            if (cols.Length < 3) continue;

            var testimonial = new Testimonial
            {
                WorkspaceId = source.WorkspaceId,
                Source = TestimonialSource.Csv,
                Status = TestimonialStatus.Approved,
                Type = TestimonialType.Text,
                Content = cols[2].Trim().Trim('"'),
                Rating = cols.Length > 3 && int.TryParse(cols[3].Trim(), out var r) ? r : null,
                Submitter = new TestimonialSubmitter
                {
                    Name = cols[0].Trim().Trim('"'),
                    Email = cols.Length > 1 ? cols[1].Trim().Trim('"') : null,
                    Company = cols.Length > 4 ? cols[4].Trim().Trim('"') : null,
                    JobTitle = cols.Length > 5 ? cols[5].Trim().Trim('"') : null
                }
            };

            _db.Testimonials.Add(testimonial);
            imported++;
        }

        source.LastSyncedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Imported {Count} testimonials from CSV source {Id}", imported, importSourceId);
    }
}
