using Newtonsoft.Json;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.Net.Http.Headers;
using System.Text;

namespace HoroscopeGroqApp
{
    public class BirthDetailsRequest
    {
        public int day { get; set; }
        public int month { get; set; }
        public int year { get; set; }
        public int hour { get; set; }
        public int min { get; set; }
        public float lat { get; set; }
        public float lon { get; set; }
        public float tzone { get; set; }
    }

    public class AstrologyApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _userId = "YOUR_USER_ID";
        private readonly string _apiKey = "YOUR_API_KEY";

        public AstrologyApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> GetBirthHoroscopeAsync(BirthDetailsRequest data, string apiname)
        {
            var url = $"https://json.astrologyapi.com/v1/{apiname}";

            string auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_userId}:{_apiKey}"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", auth);

            var json = JsonConvert.SerializeObject(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            if (!response.IsSuccessStatusCode)
            {
                var message = await response.Content.ReadAsStringAsync();
                throw new Exception("Failed to get birth details from Astrology API." + message);
            }

            return await response.Content.ReadAsStringAsync();
        }
    }

    public class GroqAiService
    {
        private readonly ChatClient openAIClient;

        public GroqAiService(string apiKey)
        {
            openAIClient = new OpenAIClient(new ApiKeyCredential(apiKey), new OpenAIClientOptions
            {
                Endpoint = new Uri("https://api.groq.com/openai/v1"),
            }).GetChatClient("meta-llama/llama-4-scout-17b-16e-instruct");
        }

        public async Task<string> InterpretAsync(string astrologyText)
        {
            try
            {
                var messages = new List<ChatMessage>
                {
                    ChatMessage.CreateSystemMessage("You are an expert astrologer. Explain birth horoscope data in a simple, spiritual, and positive way."),
                    ChatMessage.CreateUserMessage(astrologyText)
                };

                var options = new ChatCompletionOptions
                {
                    MaxOutputTokenCount = 1000,
                    Temperature = 0.7f
                };

                var result = await openAIClient.CompleteChatAsync(messages, options);

                if (result?.Value?.Content?.Count > 0)
                {
                    return result.Value.Content[0].Text;
                }

                return "No valid response from AI.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while interpreting astrology: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }
    }

    internal class Program
    {
        static async Task Main(string[] args)
        {
            var birthData = new BirthDetailsRequest
            {
                day = 19,
                month = 6,
                year = 1997,
                hour = 23,
                min = 48,
                lat = 17.4333f,
                lon = 75.2000f,
                tzone = 5.5f
            };

            var httpClient = new HttpClient();
            var astroService = new AstrologyApiService(httpClient);
            var groqService = new GroqAiService("YOUR_GROQ_API_KEY");

            try
            {
                var rawAstro = await astroService.GetBirthHoroscopeAsync(birthData, "birth_details");
                rawAstro += await astroService.GetBirthHoroscopeAsync(birthData, "astro_details");
                rawAstro += await astroService.GetBirthHoroscopeAsync(birthData, "ghat_chakra");

                var aiSummary = await groqService.InterpretAsync(rawAstro);
                Console.WriteLine("\n================ AI INTERPRETATION ================\n");
                Console.WriteLine(aiSummary);
                Console.WriteLine("\n===================================================\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }

            Console.ReadLine();
        }
    }
}

