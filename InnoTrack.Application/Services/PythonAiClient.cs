using InnoTrack.Application.DTOs.AI;
using InnoTrack.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace InnoTrack.Application.Services
{
    public class PythonAiClient : IPythonAiClient
    {
        private readonly HttpClient _httpClient;

        public PythonAiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("http://localhost:8000/api/");
        }

        public async Task<PythonAiResponseDto> AnalyzeProjectAsync(PythonAiRequestDto request)
        {
            var response = await _httpClient.PostAsJsonAsync("analyze", request);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"AI Service Error ({response.StatusCode}): {errorContent}");
            }
            var result = await response.Content.ReadFromJsonAsync<PythonAiResponseDto>();

            if (result == null)
                throw new Exception("AI Service returned an empty or invalid response.");

            return result;
        }
    }
}
