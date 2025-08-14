using System.Net;
using System.Text.Json;
namespace Dentalara.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

public class RecaptchaService
{
    private readonly HttpClient _httpClient;
    private readonly GoogleRecaptchaConfig _config;
    private readonly ILogger<RecaptchaService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RecaptchaService(
        HttpClient httpClient,
        IOptions<GoogleRecaptchaConfig> config,
        ILogger<RecaptchaService> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _config = config.Value;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<bool> VerifyToken(string token)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("Token reCAPTCHA vacío");
                return false;
            }

#if DEBUG
            _logger.LogInformation("Modo DEBUG: omitiendo verificación reCAPTCHA");
            return true;
#endif

            var remoteIp = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("secret", _config.SecretKey),
                new KeyValuePair<string, string>("response", token),
                new KeyValuePair<string, string>("remoteip", remoteIp ?? "")
            });

            var response = await _httpClient.PostAsync("", content);
            var jsonString = await response.Content.ReadAsStringAsync();

            _logger.LogInformation($"Respuesta reCAPTCHA: {jsonString}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Error en reCAPTCHA: {response.StatusCode}");
                return false;
            }

            var result = JsonSerializer.Deserialize<RecaptchaResponse>(jsonString);
            return result?.Success ?? false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al verificar reCAPTCHA");
            return false;
        }
    }

    private class RecaptchaResponse
    {
        public bool Success { get; set; }
        public List<string> ErrorCodes { get; set; }
    }
}