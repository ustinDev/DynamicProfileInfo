using System;
using ProfileInfo.DTOs;
using ProfileInfo.Services.Interfaces;

namespace ProfileInfo.Services
{
    public class CatFactService : ICatFactService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<CatFactService> _logger;
        private readonly IConfiguration _configuration;

        private const string DefaultCatFactApi = "https://catfact.ninja/fact";
        private const string FallbackFact = "Cats have over 20 vocalizations, including the meow, purr, hiss, growl, chirp, click, and grunts.";

        public CatFactService(
            HttpClient httpClient,
            ILogger<CatFactService> logger,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<string> GetRandomCatFactAsync()
        {
            const string catFactApiUrl = DefaultCatFactApi;

            try
            {
                _logger.LogInformation("Fetching cat fact from {url}", catFactApiUrl);

                var response = await _httpClient.GetAsync(catFactApiUrl);

                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadFromJsonAsync<CatFactResponseDto>();

                if (string.IsNullOrWhiteSpace(content?.Fact))
                {
                    _logger.LogWarning("Received empty cat fact from API, using fallback");
                    return FallbackFact;
                }

                _logger.LogInformation("Successfully fetched cat fact (length: {length} chars)", content.Fact.Length);
                return content.Fact;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Timeout while fetching cat fact from {url} after 10 seconds", catFactApiUrl);
                throw;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error while fetching cat fact from {url}. Status: {status}",
                    catFactApiUrl, ex.StatusCode);
                throw;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid response format from cat fact API");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching cat fact");
                throw;
            }
        }
    }
}