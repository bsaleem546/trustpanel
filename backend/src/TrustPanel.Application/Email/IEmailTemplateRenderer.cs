using TrustPanel.Domain.Email;

namespace TrustPanel.Application.Email;

public interface IEmailTemplateRenderer
{
    Task<(string Subject, string Html)> RenderAsync(
        EmailTemplateType template,
        IReadOnlyDictionary<string, string> mergeFields,
        CancellationToken cancellationToken);
}
