using MoodleIndexer.Models;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;

namespace MoodleIndexer.Services;

public class IndexService
{
    private readonly HttpClient _client = new();
    private readonly string _url;
    private readonly string _apiKey;

    public IndexService(IConfiguration config)
    {
        _url = config["MeiliSearch:Url"]!;
        _apiKey = config["MeiliSearch:ApiKey"]!;
    }

    public async Task IndexCoursesAsync(List<Course> courses)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, _url)
            {
                Content = JsonContent.Create(courses)
            };
            request.Headers.Add("Authorization", $"Bearer {_apiKey}");

            var response = await _client.SendAsync(request);
            if (response.IsSuccessStatusCode)
                Console.WriteLine(" Erfolgreich an Meilisearch gesendet.");
            else
                Console.WriteLine($" Fehler: {response.StatusCode} {await response.Content.ReadAsStringAsync()}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($" Fehler beim Senden an Meilisearch: {ex.Message}");
        }
    }
}