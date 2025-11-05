using Mentoragente.Domain.Interfaces;
using Mentoragente.Domain.Entities;
using Mentoragente.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Mentoragente.Application.Services;

public interface IMessageProcessor
{
    Task<string> ProcessMessageAsync(string phoneNumber, string messageText, Guid mentoriaId);
}

public class MessageProcessor : IMessageProcessor
{
    private readonly IUserRepository _userRepository;
    private readonly IMentoriaRepository _mentoriaRepository;
    private readonly IAgentSessionRepository _agentSessionRepository;
    private readonly IAgentSessionDataRepository _agentSessionDataRepository;
    private readonly IConversationRepository _conversationRepository;
    private readonly IOpenAIAssistantService _openAIAssistantService;
    private readonly ILogger<MessageProcessor> _logger;

    public MessageProcessor(
        IUserRepository userRepository,
        IMentoriaRepository mentoriaRepository,
        IAgentSessionRepository agentSessionRepository,
        IAgentSessionDataRepository agentSessionDataRepository,
        IConversationRepository conversationRepository,
        IOpenAIAssistantService openAIAssistantService,
        ILogger<MessageProcessor> logger)
    {
        _userRepository = userRepository;
        _mentoriaRepository = mentoriaRepository;
        _agentSessionRepository = agentSessionRepository;
        _agentSessionDataRepository = agentSessionDataRepository;
        _conversationRepository = conversationRepository;
        _openAIAssistantService = openAIAssistantService;
        _logger = logger;
    }

    public async Task<string> ProcessMessageAsync(string phoneNumber, string messageText, Guid mentoriaId)
    {
        try
        {
            // Validar mensagem vazia
            if (string.IsNullOrWhiteSpace(messageText))
            {
                _logger.LogWarning("Received empty message from {PhoneNumber}", phoneNumber);
                return "Desculpe, não consegui entender sua mensagem. Por favor, envie uma mensagem com texto.";
            }

            _logger.LogInformation("Processing message from {PhoneNumber} for mentoria {MentoriaId}: {Message}", phoneNumber, mentoriaId, messageText);

            // 1. Buscar ou criar User
            var user = await _userRepository.GetUserByPhoneAsync(phoneNumber);
            if (user == null)
            {
                user = new User
                {
                    PhoneNumber = phoneNumber,
                    Name = "Cliente WhatsApp",
                    Status = UserStatus.Active
                };
                user = await _userRepository.CreateUserAsync(user);
                _logger.LogInformation("Created new user {UserId} for phone {PhoneNumber}", user.Id, phoneNumber);
            }

            // 2. Buscar Mentoria
            var mentoria = await _mentoriaRepository.GetMentoriaByIdAsync(mentoriaId);
            if (mentoria == null)
            {
                _logger.LogError("Mentoria {MentoriaId} not found", mentoriaId);
                throw new InvalidOperationException($"Mentoria {mentoriaId} not found");
            }

            // 3. Buscar ou criar AgentSession
            var agentSession = await _agentSessionRepository.GetActiveAgentSessionAsync(user.Id, mentoriaId);
            if (agentSession == null)
            {
                // Verificar se existe sessão expirada
                var existingSession = await _agentSessionRepository.GetAgentSessionAsync(user.Id, mentoriaId);
                
                if (existingSession != null)
                {
                    // Verificar acesso
                    var sessionData = await _agentSessionDataRepository.GetAgentSessionDataAsync(existingSession.Id);
                    if (sessionData != null && DateTime.UtcNow > sessionData.AccessEndDate)
                    {
                        // Acesso expirado
                        existingSession.Status = AgentSessionStatus.Expired;
                        await _agentSessionRepository.UpdateAgentSessionAsync(existingSession);
                        _logger.LogWarning("Access expired for user {UserId} in mentoria {MentoriaId}", user.Id, mentoriaId);
                        return "Seu período de acesso a esta mentoria terminou. Entre em contato para renovar.";
                    }
                    else if (sessionData != null && DateTime.UtcNow <= sessionData.AccessEndDate)
                    {
                        // Reativar sessão
                        existingSession.Status = AgentSessionStatus.Active;
                        agentSession = await _agentSessionRepository.UpdateAgentSessionAsync(existingSession);
                    }
                }

                // Criar nova sessão se não encontrou
                if (agentSession == null)
                {
                    agentSession = new AgentSession
                    {
                        UserId = user.Id,
                        MentoriaId = mentoriaId,
                        Status = AgentSessionStatus.Active
                    };
                    agentSession = await _agentSessionRepository.CreateAgentSessionAsync(agentSession);

                    // Criar AgentSessionData
                    var sessionData = new AgentSessionData
                    {
                        AgentSessionId = agentSession.Id,
                        AccessStartDate = DateTime.UtcNow,
                        AccessEndDate = DateTime.UtcNow.AddDays(mentoria.DuracaoDias),
                        ProgressPercentage = 0
                    };
                    await _agentSessionDataRepository.CreateAgentSessionDataAsync(sessionData);
                    _logger.LogInformation("Created new agent session {AgentSessionId} for user {UserId} and mentoria {MentoriaId}", agentSession.Id, user.Id, mentoriaId);
                }
            }

            // 4. Validar acesso
            var accessData = await _agentSessionDataRepository.GetAgentSessionDataAsync(agentSession.Id);
            if (accessData == null)
            {
                _logger.LogError("AgentSessionData not found for session {AgentSessionId}", agentSession.Id);
                throw new InvalidOperationException("Session data not found");
            }

            if (DateTime.UtcNow > accessData.AccessEndDate)
            {
                agentSession.Status = AgentSessionStatus.Expired;
                await _agentSessionRepository.UpdateAgentSessionAsync(agentSession);
                return "Seu período de acesso a esta mentoria terminou. Entre em contato para renovar.";
            }

            // 5. Criar Thread ID se não existir
            if (string.IsNullOrEmpty(agentSession.AIContextId))
            {
                var threadId = await _openAIAssistantService.CreateThreadAsync();
                agentSession.AIContextId = threadId;
                agentSession = await _agentSessionRepository.UpdateAgentSessionAsync(agentSession);
                _logger.LogInformation("Created OpenAI thread {ThreadId} for agent session {AgentSessionId}", threadId, agentSession.Id);
            }

            // 6. Adicionar mensagem do usuário ao histórico local
            await _conversationRepository.AddMessageAsync(agentSession.Id, "user", messageText);

            // 7. Enviar mensagem para OpenAI Assistant
            if (string.IsNullOrEmpty(agentSession.AIContextId))
            {
                throw new InvalidOperationException($"AgentSession {agentSession.Id} has no AIContextId after validation");
            }
            await _openAIAssistantService.AddUserMessageAsync(agentSession.AIContextId, messageText);

            // 8. Buscar resposta do assistente
            var responseText = await _openAIAssistantService.RunAssistantAsync(agentSession.AIContextId, mentoria.AssistantId);

            // 9. Adicionar resposta ao histórico local
            await _conversationRepository.AddMessageAsync(agentSession.Id, "assistant", responseText);

            // 10. Atualizar sessão
            agentSession.LastInteraction = DateTime.UtcNow;
            agentSession.TotalMessages += 2; // user + assistant
            await _agentSessionRepository.UpdateAgentSessionAsync(agentSession);

            // 11. Atualizar progresso (opcional - pode calcular baseado em mensagens)
            accessData.ProgressPercentage = Math.Min(100, (agentSession.TotalMessages * 100) / (mentoria.DuracaoDias * 10)); // Estimativa
            await _agentSessionDataRepository.UpdateAgentSessionDataAsync(accessData);

            _logger.LogInformation("Successfully processed message from {PhoneNumber} for mentoria {MentoriaId}", phoneNumber, mentoriaId);

            return responseText;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message from {PhoneNumber}", phoneNumber);
            throw;
        }
    }
}

