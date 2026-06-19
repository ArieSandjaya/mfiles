namespace MFilesClone.Services;

public interface IDocumentImportPrompt
{
    Task<bool> PromptAndImportAsync(string sourceFilePath, int? categoryId);
}
