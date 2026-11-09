using Portfolio.Application.Models;

namespace Portfolio.Application.Interfaces;

public interface IImportPreviewStore
{
    ImportPreviewSession? Get(string previewToken);
    void Set(string previewToken, ImportPreviewSession session, TimeSpan? slidingExpiration = null);
    void Remove(string previewToken);
}

