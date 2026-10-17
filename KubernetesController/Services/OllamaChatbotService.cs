using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace KubernetesController.Services
{
    public class OllamaChatbotService
    {
        private readonly string baseUrl;
        private readonly string model;

        public string BaseUrl
        {
            get { return baseUrl; }
        }

        public string Model
        {
            get { return model; }
        }

        public OllamaChatbotService(string baseUrl, string model)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
                baseUrl = "http://localhost:11434";

            this.baseUrl = baseUrl.TrimEnd('/');
            this.model = string.IsNullOrWhiteSpace(model) ? "llama3.2:1b" : model.Trim();
        }

        public async Task<bool> CheckAvailabilityAsync()
        {
            using (HttpClient client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(5);

                // GET automático à VM:
                // este pedido consulta o endereço base do Ollama, por exemplo http://IP_DA_VM:11434/.
                // Serve apenas para validar se o ChatBot local está acessível.
                // Não lança mensagens de erro ao arrancar a aplicação; o estado é mostrado na aba ChatBot.
                using (HttpResponseMessage response = await client.GetAsync(baseUrl + "/"))
                {
                    return response.IsSuccessStatusCode;
                }
            }
        }

        public async Task<string> AskAsync(string prompt)
        {
            using (HttpClient client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(120);

                Dictionary<string, object> requestBody = new Dictionary<string, object>();
                requestBody["model"] = model;
                requestBody["prompt"] = prompt;
                requestBody["stream"] = false;

                string json = JsonSerializer.Serialize(requestBody);

                using (HttpContent content = new StringContent(json, Encoding.UTF8, "application/json"))
                using (HttpResponseMessage response = await client.PostAsync(baseUrl + "/api/generate", content))
                {
                    string responseText = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                        throw new Exception("Erro ao comunicar com o ChatBot: " + response.StatusCode + "\n" + responseText);

                    using (JsonDocument doc = JsonDocument.Parse(responseText))
                    {
                        JsonElement root = doc.RootElement;

                        JsonElement error;
                        if (root.TryGetProperty("error", out error))
                            throw new Exception(error.GetString() ?? "Erro devolvido pelo Ollama.");

                        JsonElement message;
                        if (root.TryGetProperty("response", out message))
                            return message.GetString() ?? "";
                    }

                    return responseText;
                }
            }
        }
    }
}
