using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QuizSystem.Application.Contracts.Exams;
using QuizSystem.Application.DTOs;

namespace QuizSystem.Infrastructure.Services.Exams;

public class OpenAiQuestionGenerator : IAiQuestionGenerator
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OpenAiQuestionGenerator> _logger;

    public OpenAiQuestionGenerator(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<OpenAiQuestionGenerator> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> SummarizeEducationalContentAsync(
        string educationalContent,
        string? examContext = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(educationalContent))
            throw new InvalidOperationException("تعذر تلخيص ملف PDF لأنه لا يحتوي على نص قابل للقراءة.");

        var apiKey = ResolveApiKey();
        var model = _configuration["OpenAI:Model"] ?? "gpt-4o-mini";
        var baseUrl = _configuration["OpenAI:BaseUrl"] ?? "https://api.openai.com/v1/";
        var payload = new
        {
            model,
            temperature = 0.2,
            messages = new object[]
            {
                new
                {
                    role = "system",
                    content = "أنت خبير تربوي. لخّص المحتوى التعليمي بدقة وبنفس لغته، وحافظ على الحقائق والمصطلحات والتعريفات والقواعد والأمثلة التي تصلح لبناء أسئلة قابلة للقياس. لا تضف معلومات غير موجودة في المصدر."
                },
                new
                {
                    role = "user",
                    content = $"""
                    سياق الاختبار:
                    {examContext ?? "غير محدد"}

                    لخّص النص التالي في ملخص تعليمي منظم لا يتجاوز 6000 حرف. غطّ جميع الأفكار الرئيسية، والمفاهيم، والعلاقات، والأمثلة المهمة، والنقاط التي يمكن تقييمها باختبار:

                    {educationalContent}
                    """
                }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/"), "chat/completions"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new InvalidOperationException("استغرق تلخيص ملف PDF وقتاً أطول من المتوقع. جرّب ملفاً أصغر ثم أعد المحاولة.");
        }

        using (response)
        {
            var raw = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("OpenAI summarization failed: {StatusCode} - {Body}", response.StatusCode, raw);
                throw new InvalidOperationException("تعذر تلخيص المحتوى التعليمي بواسطة الذكاء الاصطناعي.");
            }

            var summary = ExtractMessageContent(raw).Trim();
            if (string.IsNullOrWhiteSpace(summary))
                throw new InvalidOperationException("لم يتمكن الذكاء الاصطناعي من إعداد ملخص للمحتوى التعليمي.");

            return summary.Length <= 6000 ? summary : summary[..6000];
        }
    }

    public async Task<List<GeneratedQuestionDto>> GenerateQuestionsAsync(
        string topic,
        int count,
        string? educationalContent = null,
        string? blueprintInstructions = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(topic))
            throw new InvalidOperationException("Topic is required for AI generation");

        if (count <= 0)
            throw new InvalidOperationException("Question count must be greater than zero");

        var apiKey = ResolveApiKey();
        var model = _configuration["OpenAI:Model"] ?? "gpt-4o-mini";
        var baseUrl = _configuration["OpenAI:BaseUrl"] ?? "https://api.openai.com/v1/";
        var temperature = ResolveTemperature();

        var prompt = BuildPrompt(topic, count, educationalContent, blueprintInstructions);

        var payload = new
        {
            model = model,
            temperature = temperature,
            response_format = new { type = "json_object" },
            messages = new object[]
            {
                new
                {
                    role = "system",
                    content = "أنت خبير تربوي متخصص في إنشاء أسئلة اختيار من متعدد. استخدم لغة وصف الاختبار والمحتوى التعليمي المصدر، مع الحفاظ على المصطلحات كما وردت. أعد JSON صالحًا فقط دون أي شرح إضافي."
                },
                new
                {
                    role = "user",
                    content = prompt
                }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/"), "chat/completions"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json"
        );

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new InvalidOperationException("استغرق مزود الذكاء الاصطناعي وقتًا أطول من المتوقع. قلل عدد الأسئلة أو حجم ملف PDF ثم أعد المحاولة");
        }
        using (response)
        {
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("OpenAI request failed: {StatusCode} - {Body}", response.StatusCode, raw);
            throw new InvalidOperationException("OpenAI request failed");
        }

        var content = ExtractMessageContent(raw);
        var questions = ParseQuestions(content);

        if (questions.Count == 0)
            throw new InvalidOperationException("AI returned zero questions");

        if (questions.Count < count)
            _logger.LogWarning("AI returned fewer questions than requested. Requested={Requested}, Actual={Actual}", count, questions.Count);

        ValidateQuestions(questions);

        return questions;
        }
    }

    private string ResolveApiKey()
    {
        var envKey =
            Environment.GetEnvironmentVariable("OPENAI_API_KEY") ??
            Environment.GetEnvironmentVariable("OpenAI__ApiKey");

        var configKey = _configuration["OpenAI:ApiKey"];

        var key = !string.IsNullOrWhiteSpace(envKey) ? envKey : configKey;

        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("خدمة توليد الأسئلة بالذكاء الاصطناعي غير مهيأة. أضف مفتاح OPENAI_API_KEY في إعدادات الخادم ثم أعد المحاولة");

        return key;
    }

    private double ResolveTemperature()
    {
        var raw = _configuration["OpenAI:Temperature"];
        return double.TryParse(raw, out var t) ? t : 0.4;
    }

    private static string BuildPrompt(string topic, int count, string? educationalContent, string? blueprintInstructions)
    {
        return $@"
أنشئ {count} سؤال اختيار من متعدد عالي الجودة اعتمادًا حصريًا على وصف الاختبار والمحتوى التعليمي المرفق.

وصف وموضوع الاختبار:
{topic}

مخطط ورقة الاختبار المطلوب:
{blueprintInstructions ?? "توزيع متوازن على مستويات Bloom، وبدون CLO ما لم يذكر خلاف ذلك."}

الملخص التعليمي المُعد أولاً من ملف PDF:
{(string.IsNullOrWhiteSpace(educationalContent) ? "لا يوجد ملف؛ اعتمد على وصف الاختبار." : educationalContent)}

الشروط:
- الأسئلة حقيقية وتعليمية وليست عامة أو placeholder
- كل سؤال يحتوي 4 اختيارات فقط
- إجابة صحيحة واحدة فقط
- لا تكرر نفس الفكرة
- التزم بعدد الأسئلة وتوزيع CLO وBloom المذكور قدر الإمكان
- cloCode يكون رمز CLO المطلوب، أو null للأسئلة غير المرتبطة
- cognitiveLevel يجب أن يكون أحد: Remember, Understand, Apply, Analyze, Evaluate, Create
- اجعل مستوى الأسئلة مناسبًا للتعليم المدرسي العام
- استخدم اللغة الأساسية لوصف الاختبار والمحتوى التعليمي في جميع الحقول، واجعل correctAnswer قيمة A أو B أو C أو D
- لا تستخدم نصوصًا مثل Choice A أو AI Question أو Generated explanation

أعد JSON فقط بهذا الشكل:
{{
  ""questions"": [
    {{
      ""questionText"": ""..."",
      ""cloCode"": null,
      ""cognitiveLevel"": ""Understand"",
      ""choiceA"": ""..."",
      ""choiceB"": ""..."",
      ""choiceC"": ""..."",
      ""choiceD"": ""..."",
      ""correctAnswer"": ""A"",
      ""explanation"": ""...""
    }}
  ]
}}
";
    }

    private static string ExtractMessageContent(string raw)
    {
        using var doc = JsonDocument.Parse(raw);
        return doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "{}";
    }

    private static List<GeneratedQuestionDto> ParseQuestions(string content)
    {
        try
        {
            using var doc = JsonDocument.Parse(content);

            if (!doc.RootElement.TryGetProperty("questions", out var questionsNode))
                return new List<GeneratedQuestionDto>();

            var result = JsonSerializer.Deserialize<List<GeneratedQuestionDto>>(
                questionsNode.GetRawText(),
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            return result ?? new List<GeneratedQuestionDto>();
        }
        catch
        {
            return new List<GeneratedQuestionDto>();
        }
    }

    private static void ValidateQuestions(List<GeneratedQuestionDto> questions)
    {
        foreach (var q in questions)
        {
            if (string.IsNullOrWhiteSpace(q.QuestionText))
                throw new InvalidOperationException("AI returned question with empty QuestionText");

            if (string.IsNullOrWhiteSpace(q.ChoiceA) ||
                string.IsNullOrWhiteSpace(q.ChoiceB) ||
                string.IsNullOrWhiteSpace(q.ChoiceC) ||
                string.IsNullOrWhiteSpace(q.ChoiceD))
                throw new InvalidOperationException("AI returned incomplete choices");

            var answer = (q.CorrectAnswer ?? string.Empty).Trim().ToUpperInvariant();
            if (answer is not ("A" or "B" or "C" or "D"))
                throw new InvalidOperationException("AI returned invalid correctAnswer");

            q.CorrectAnswer = answer;
            q.QuestionText = q.QuestionText.Trim();
            q.ChoiceA = q.ChoiceA.Trim();
            q.ChoiceB = q.ChoiceB.Trim();
            q.ChoiceC = q.ChoiceC.Trim();
            q.ChoiceD = q.ChoiceD.Trim();
            q.Explanation = (q.Explanation ?? string.Empty).Trim();
            q.CognitiveLevel = q.CognitiveLevel is "Remember" or "Understand" or "Apply" or "Analyze" or "Evaluate" or "Create"
                ? q.CognitiveLevel : "Understand";
            q.CloCode = string.IsNullOrWhiteSpace(q.CloCode) ? null : q.CloCode.Trim();
        }
    }
}
