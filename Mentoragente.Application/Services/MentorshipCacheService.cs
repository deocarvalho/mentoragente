using Mentoragente.Domain.Entities;
using Mentoragente.Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Mentoragente.Application.Services;

public interface IMentorshipCacheService
{
    Task<Mentorship?> GetMentorshipAsync(Guid mentorshipId);
    void InvalidateMentorship(Guid mentorshipId);
}

public class MentorshipCacheService : IMentorshipCacheService
{
    private readonly IMentorshipRepository _mentorshipRepository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<MentorshipCacheService> _logger;
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);

    public MentorshipCacheService(
        IMentorshipRepository mentorshipRepository,
        IMemoryCache cache,
        ILogger<MentorshipCacheService> logger)
    {
        _mentorshipRepository = mentorshipRepository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Mentorship?> GetMentorshipAsync(Guid mentorshipId)
    {
        var cacheKey = GetCacheKey(mentorshipId);
        
        if (_cache.TryGetValue(cacheKey, out Mentorship? cachedMentorship))
        {
            _logger.LogDebug("Mentorship {MentorshipId} retrieved from cache", mentorshipId);
            return cachedMentorship;
        }

        var mentorship = await _mentorshipRepository.GetMentorshipByIdAsync(mentorshipId);
        
        if (mentorship != null)
        {
            // Use sliding expiration: cache expires 5 minutes after last access
            // This keeps active mentorships in cache longer while allowing inactive ones to expire
            var cacheOptions = new MemoryCacheEntryOptions
            {
                SlidingExpiration = CacheExpiration,
                // Also set absolute expiration as a safety net (max 30 minutes)
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
            };
            
            _cache.Set(cacheKey, mentorship, cacheOptions);
            _logger.LogDebug("Mentorship {MentorshipId} cached with sliding expiration of {Minutes} minutes", mentorshipId, CacheExpiration.TotalMinutes);
        }

        return mentorship;
    }

    public void InvalidateMentorship(Guid mentorshipId)
    {
        var cacheKey = GetCacheKey(mentorshipId);
        _cache.Remove(cacheKey);
        _logger.LogDebug("Mentorship {MentorshipId} cache invalidated", mentorshipId);
    }

    private static string GetCacheKey(Guid mentorshipId) => $"mentorship_{mentorshipId}";
}

