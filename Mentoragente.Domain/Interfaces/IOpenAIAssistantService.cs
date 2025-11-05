namespace Mentoragente.Domain.Interfaces;

public interface IOpenAIAssistantService
{
    Task<string> CreateThreadAsync();
    Task AddUserMessageAsync(string threadId, string userMessage);
    Task<string> RunAssistantAsync(string threadId, string assistantId);
}

