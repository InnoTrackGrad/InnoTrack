using InnoTrack.Application.DTOs.AI;
using InnoTrack.Application.Exceptions;
using InnoTrack.Application.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace InnoTrack.Application.Services
{
    public class PythonAiClient : IPythonAiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<PythonAiClient> _logger;

        public PythonAiClient(HttpClient httpClient, ILogger<PythonAiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<PythonAiResponseDto> AnalyzeProjectAsync(PythonAiRequestDto request)
        {
            var response = await _httpClient.PostAsJsonAsync("analyze", request);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new ExternalServiceException($"AI Service Error ({response.StatusCode}): {errorContent}");
            }

            var result = await response.Content.ReadFromJsonAsync<PythonAiResponseDto>();

            if (result == null)
                throw new ExternalServiceException("AI Service returned an empty or invalid response.");

            return result;
        }

        public async Task<GenerateAbstractResponseDto> GenerateProjectAbstractAsync(GenerateAbstractRequestDto request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/generate-abstract", request);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("AI Service returned {StatusCode} when generating abstract.", response.StatusCode);
                    throw new Exception("AI Service is currently unavailable. Please try again later.");
                }

                var result = await response.Content.ReadFromJsonAsync<GenerateAbstractResponseDto>();
                if (result == null || string.IsNullOrWhiteSpace(result.GeneratedAbstract))
                {
                    throw new Exception("AI Service returned an empty abstract.");
                }

                return result;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to connect to the Python AI microservice.");
                throw new ExternalServiceException("Could not connect to the AI Assistant. Make sure the AI service is running.");
            }
        }
    }
}
