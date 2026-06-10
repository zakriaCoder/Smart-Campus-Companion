using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SmartCampus.Services;

public sealed class AiAssistantService
{
    private readonly HttpClient httpClient;
    private readonly IConfiguration configuration;
    private readonly CampusRealtimeService campus;

    public AiAssistantService(HttpClient httpClient, IConfiguration configuration, CampusRealtimeService campus)
    {
        this.httpClient = httpClient;
        this.configuration = configuration;
        this.campus = campus;
    }

    public async Task<AiAssistantReply> AskAsync(string question, string role, CancellationToken cancellationToken = default)
    {
        question = question.Trim();
        if (string.IsNullOrWhiteSpace(question))
            return new AiAssistantReply("Ask me about classes, rides, events, attendance, resources, or campus help.", false);

        var prompt = BuildSystemPrompt(role);

        var geminiApiKey = configuration["Gemini:ApiKey"];
        if (!string.IsNullOrWhiteSpace(geminiApiKey))
            return await AskGeminiAsync(question, prompt, geminiApiKey, cancellationToken);

        var apiKey = configuration["OpenAI:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            return new AiAssistantReply(BuildLocalReply(question, role), false);

        return await AskOpenAiAsync(question, prompt, role, apiKey, cancellationToken);
    }

    private async Task<AiAssistantReply> AskGeminiAsync(string question, string prompt, string apiKey, CancellationToken cancellationToken)
    {
        var model = configuration["Gemini:Model"];
        if (string.IsNullOrWhiteSpace(model))
            model = "gemini-3.5-flash";

        var body = JsonSerializer.Serialize(new
        {
            system_instruction = new
            {
                parts = new[] { new { text = prompt } }
            },
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[] { new { text = question } }
                }
            },
            generationConfig = new
            {
                maxOutputTokens = 320,
                temperature = 0.45
            }
        });

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(model)}:generateContent");
        request.Headers.Add("x-goog-api-key", apiKey);
        request.Content = new StringContent(body, Encoding.UTF8, "application/json");

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                return new AiAssistantReply($"Gemini AI is configured, but the request failed: {response.StatusCode}. Check Gemini:ApiKey and Gemini:Model.", false);

            var text = ExtractGeminiText(json);
            return new AiAssistantReply(string.IsNullOrWhiteSpace(text) ? BuildLocalReply(question, "campus") : text, true);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new AiAssistantReply("Gemini AI is configured, but I could not reach it right now. I can still help with built-in campus data.", false);
        }
    }

    private async Task<AiAssistantReply> AskOpenAiAsync(string question, string prompt, string role, string apiKey, CancellationToken cancellationToken)
    {
        var model = configuration["OpenAI:Model"];
        if (string.IsNullOrWhiteSpace(model))
            model = "gpt-4.1-mini";

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var body = JsonSerializer.Serialize(new
        {
            model,
            input = new object[]
            {
                new { role = "system", content = prompt },
                new { role = "user", content = question }
            }
        });

        request.Content = new StringContent(body, Encoding.UTF8, "application/json");

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                return new AiAssistantReply($"AI service is configured, but the request failed: {response.StatusCode}. Check your API key and model name.", false);

            var text = ExtractOutputText(json);
            return new AiAssistantReply(string.IsNullOrWhiteSpace(text) ? BuildLocalReply(question, role) : text, true);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new AiAssistantReply("I could not reach the AI service right now. I can still help with the built-in campus data.", false);
        }
    }

    private string BuildSystemPrompt(string role) => $"""
    You are SmartCampus Companion, a concise and friendly assistant inside a Blazor .NET campus portal.
    Help the signed-in user with campus tasks, schedules, ride sharing, study resources, events, lost/found,
    attendance, assignments, course registration, and general project guidance.

    User role: {role}
    Current portal snapshot:
    - Available rides: {campus.AvailableRides}
    - Events: {campus.Events.Count}
    - Study resources: {campus.Resources.Count}
    - Notifications: {campus.Notifications.Count}
    - Marketplace items: {campus.Marketplace.Count}
    - Open lost/found reports: {campus.OpenLostFound}
    - Active courses: {campus.ActiveCourses}

    Answer like a helpful campus assistant, not a generic bot. Mention exact portal pages when useful.
    Keep answers simple, practical, and under 120 words unless the user asks for detail.
    """;

    private string BuildLocalReply(string question, string role)
    {
        var lower = question.ToLowerInvariant();

        if (lower.Contains("ride") || lower.Contains("car") || lower.Contains("home"))
            return $"There are {campus.AvailableRides} ride options with seats available. Open Ride Sharing, enter pickup and destination, then request or book a matching route.";

        if (lower.Contains("event"))
            return $"There are {campus.Events.Count} campus events in the portal. Check Events to register, view venue, and see capacity.";

        if (lower.Contains("assignment"))
            return "Open Assignments to view deadlines and submission status. Faculty users can manage assignments and review submissions from the faculty dashboard.";

        if (lower.Contains("attendance"))
            return "Attendance is available from the dashboard. Students can view records, while faculty can mark and review attendance by course.";

        if (lower.Contains("resource") || lower.Contains("study"))
            return $"The portal currently has {campus.Resources.Count} study resources. Use Resources to search course notes, files, and uploaded material.";

        if (lower.Contains("lost") || lower.Contains("found"))
            return "Use Lost & Found to report an item with title, category, location, and description. The portal sends a live alert after reporting.";

        return $"I can help {role.ToLowerInvariant()} users with SmartCampus workflows. For live AI answers, add a free Gemini API key as Gemini:ApiKey in deployment settings. Until then, I can still guide rides, events, resources, attendance, assignments, and lost/found from the portal data.";
    }

    private static string ExtractGeminiText(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (!root.TryGetProperty("candidates", out var candidates) || candidates.ValueKind != JsonValueKind.Array)
            return "";

        var builder = new StringBuilder();
        foreach (var candidate in candidates.EnumerateArray())
        {
            if (!candidate.TryGetProperty("content", out var content))
                continue;

            if (!content.TryGetProperty("parts", out var parts) || parts.ValueKind != JsonValueKind.Array)
                continue;

            foreach (var part in parts.EnumerateArray())
            {
                if (part.TryGetProperty("text", out var text))
                    builder.Append(text.GetString());
            }
        }

        return builder.ToString();
    }

    private static string ExtractOutputText(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (root.TryGetProperty("output_text", out var outputText))
            return outputText.GetString() ?? "";

        if (!root.TryGetProperty("output", out var output) || output.ValueKind != JsonValueKind.Array)
            return "";

        var builder = new StringBuilder();
        foreach (var item in output.EnumerateArray())
        {
            if (!item.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
                continue;

            foreach (var part in content.EnumerateArray())
            {
                if (part.TryGetProperty("text", out var text))
                    builder.Append(text.GetString());
            }
        }

        return builder.ToString();
    }
}

public sealed record AiAssistantReply(string Text, bool UsedRemoteModel);
