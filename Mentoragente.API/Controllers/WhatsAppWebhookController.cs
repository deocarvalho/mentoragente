using Microsoft.AspNetCore.Mvc;
using Mentoragente.Application.Services;
using Mentoragente.Domain.Interfaces;
using Mentoragente.Domain.Models;
using Mentoragente.Domain.Entities;
using System.Text.Json;

namespace Mentoragente.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WhatsAppWebhookController : ControllerBase
{
    private readonly IMessageProcessor _messageProcessor;
    private readonly IEvolutionAPIService _evolutionAPIService;
    private readonly IMentoriaRepository _mentoriaRepository;
    private readonly ILogger<WhatsAppWebhookController> _logger;

    public WhatsAppWebhookController(
        IMessageProcessor messageProcessor,
        IEvolutionAPIService evolutionAPIService,
        IMentoriaRepository mentoriaRepository,
        ILogger<WhatsAppWebhookController> logger)
    {
        _messageProcessor = messageProcessor;
        _evolutionAPIService = evolutionAPIService;
        _mentoriaRepository = mentoriaRepository;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> ReceiveMessage([FromBody] WhatsAppWebhookDto webhook)
    {
        try
        {
            _logger.LogInformation("Received webhook: {webhook}", JsonSerializer.Serialize(webhook));

            if (webhook.Event == "messages.upsert" && webhook.Data != null)
            {
                // Ignorar mensagens enviadas por você
                if (webhook.Data.Key?.FromMe == true)
                {
                    _logger.LogInformation("Ignoring message from self");
                    return Ok(new { success = true, message = "Message from self ignored" });
                }

                var remoteJid = webhook.Data.Key?.RemoteJid ?? string.Empty;
                var messageText = webhook.Data.Message?.Conversation ?? string.Empty;

                if (string.IsNullOrEmpty(messageText))
                {
                    _logger.LogWarning("Received empty message from {RemoteJid}", remoteJid);
                    return Ok(new { success = true, message = "Empty message ignored" });
                }

                // Extrair phone number do JID
                var phoneNumber = ExtractPhoneNumber(remoteJid);
                if (string.IsNullOrEmpty(phoneNumber))
                {
                    _logger.LogWarning("Could not extract phone number from {RemoteJid}", remoteJid);
                    return Ok(new { success = true, message = "Invalid phone number format" });
                }

                _logger.LogInformation("Processing message from {PhoneNumber}: {Message}", phoneNumber, messageText);

                // Buscar mentoria (via query param ou primeira ativa)
                var mentoriaId = await GetMentoriaIdFromRequestAsync();

                // Processar mensagem usando MessageProcessor
                var chatResponse = await _messageProcessor.ProcessMessageAsync(phoneNumber, messageText, mentoriaId);

                // Enviar resposta de volta via WhatsApp
                var success = await _evolutionAPIService.SendMessageAsync(phoneNumber, chatResponse);

                if (success)
                {
                    _logger.LogInformation("Successfully processed message from {PhoneNumber}", phoneNumber);
                    return Ok(new { success = true, message = "Message processed successfully" });
                }
                else
                {
                    _logger.LogError("Failed to send response to {PhoneNumber}", phoneNumber);
                    return BadRequest(new { success = false, message = "Failed to send response" });
                }
            }

            _logger.LogInformation("Webhook received but no message to process");
            return Ok(new { success = true, message = "Webhook received" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    private string ExtractPhoneNumber(string remoteJid)
    {
        if (string.IsNullOrWhiteSpace(remoteJid))
            return string.Empty;

        // Formato: "5511999999999@s.whatsapp.net" ou "5511999999999:5511999999999@s.whatsapp.net"
        var phonePart = remoteJid.Split('@')[0];
        
        // Se tiver dois pontos, pegar a primeira parte
        if (phonePart.Contains(':'))
        {
            phonePart = phonePart.Split(':')[0];
        }

        // Remover tudo exceto dígitos
        return new string(phonePart.Where(char.IsDigit).ToArray());
    }

    private async Task<Guid> GetMentoriaIdFromRequestAsync()
    {
        // 1. Tentar buscar via query parameter
        var mentoriaIdParam = Request.Query["mentoriaId"].FirstOrDefault();
        if (!string.IsNullOrEmpty(mentoriaIdParam) && Guid.TryParse(mentoriaIdParam, out var mentoriaId))
        {
            var mentoria = await _mentoriaRepository.GetMentoriaByIdAsync(mentoriaId);
            if (mentoria != null)
            {
                return mentoriaId;
            }
        }

        // 2. Se não fornecido, buscar primeira mentoria ativa (fallback)
        // TODO: Melhorar isso para buscar baseado em configuração ou mapping
        // Por enquanto, retornar erro se não encontrar
        throw new InvalidOperationException(
            "MentoriaId must be provided via query parameter '?mentoriaId=xxx'. " +
            "Multiple mentorias support coming soon.");
    }
}

