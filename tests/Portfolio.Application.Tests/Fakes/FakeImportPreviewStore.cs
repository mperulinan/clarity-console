using Portfolio.Application.Interfaces;
using Portfolio.Application.Models;

namespace Portfolio.Application.Tests.Fakes;

public class FakeImportPreviewStore : IImportPreviewStore
{
    private readonly Dictionary<string, ImportPreviewSession> _sessions = new();

    public ImportPreviewSession? Get(string previewToken) =>
        _sessions.GetValueOrDefault(previewToken);

    public void Set(string previewToken, ImportPreviewSession session, TimeSpan? slidingExpiration = null) =>
        _sessions[previewToken] = session;

    public void Remove(string previewToken) =>
        _sessions.Remove(previewToken);
}

