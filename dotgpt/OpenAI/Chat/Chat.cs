using System.Text.Json;

namespace dotgpt.OpenAI.Chat
{
    public class Message
    {
        public string role { get; set; } = default!;
        public string content { get; set; } = default!;
    }

    public class CompletionResponse
    {
        public class Usage
        {
            public int prompt_tokens { get; set; } = default!;
            public int completion_tokens { get; set; } = default!;
            public int total_tokens { get; set; } = default!;
        }

        public class Choice
        {
            public Message message { get; set; } = default!;
            public Message delta { get; set; } = default!;      // when using event streams
            public int index { get; set; } = default!;
            public string finish_reason { get; set; } = default!;
        }

        public string id { get; set; } = default!;

        public long created { get; set; } = default!;

        public string model { get; set; } = default!;

        public Usage usage { get; set; } = default!;

        public List<Choice> choices { get; set; } = default!;
    }

    //-----------------------------------------------
    // Session
    //-----------------------------------------------
    public class Session
    {
        public string Name { get; set; } = "default";

        public List<Message> History { get; set; } = new List<Message>();

        public string APIKey { protected get; set; } = default!;

        public string Instructions { protected get; set; } = "You are a helpful AI assistant. Answer as concisely as possible.";

        public double Temperature { protected get; set; } = 0.5;

        public int MaxTokens { protected get; set; } = 1024;

        public string Model { protected get; set; } = "gpt-3.5-turbo";

        public int PromptHistory { protected get; set; } = 5;

        //-----------------------------------------------
        // Session::Session
        //-----------------------------------------------
        public Session(string aPIKey)
        {
            this.APIKey = aPIKey;
        }

        //-----------------------------------------------
        // Session::Load
        //-----------------------------------------------
        public static Session? Load
            (
                string sessionName
            )
        {
            Session? session = null;
            try
            {
                string filename = $"{dotgpt.Utils.GetApplicationDataPath()}Sessions/{sessionName}.json";

                if (File.Exists(filename))
                {
                    string fileContent = File.ReadAllText(filename);
                    session = JsonSerializer.Deserialize<Session>(fileContent);
                }
            }
            catch (Exception)
            {
            }

            return session;
        }

        //-----------------------------------------------
        // Session::Save
        //-----------------------------------------------
        public void Save()
        {
            string s = JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            try
            {
                string filename = $"{dotgpt.Utils.GetApplicationDataPath()}Sessions/{this.Name}.json";

                string? directoryPath = Path.GetDirectoryName(filename);
                if (directoryPath != null && !Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                File.WriteAllText(filename, s);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        //-----------------------------------------------
        // Session::EnterPrompt
        //-----------------------------------------------
        public async Task<Message> EnterPrompt
            (
                string prompt,
                Action<string> onRoleChanged,
                Action<string> onToken,
                Action<string> onError
            )
        {
            Message userMessage = new Message() { role = "user", content = prompt };
            Message responseMessage = new Message() { role = "assistant" };

            // assemble prompt messages to send
            List<Message> promptMessages = new List<Message>();
            {
                // instructions
                promptMessages.Add(new Message() { role = "system", content = this.Instructions });

                // previous history
                int numPromptsToReuse = this.PromptHistory < this.History.Count ? this.PromptHistory : this.History.Count;
                for (int i = numPromptsToReuse; i > 0; i--)
                {
                    promptMessages.Add(this.History[this.History.Count - i]);
                }

                // new user prompt
                promptMessages.Add(new Message() { role = "user", content = prompt });
            }

            // Send a request to the chat completions endpoint to generate the completion asynchronously
            HttpRequestMessage? request = null;
            try
            {
                request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri("https://api.openai.com/v1/chat/completions"),
                    Headers =
                    {
                        { "Authorization", $"Bearer {this.APIKey}" }
                    },
                    Content = new StringContent(JsonSerializer.Serialize(new
                    {
                        model = this.Model,
                        messages = promptMessages.ToArray(),
                        max_tokens = this.MaxTokens,
                        temperature = this.Temperature,
                        stream = true
                    }),
                    System.Text.Encoding.UTF8, "application/json")
                };
            }
            catch (Exception e)
            {
                if (onError != null)
                {
                    onError(e.Message);
                }

                return new Message() { role = "Exception", content = e.Message };
            }

            // response will come in in the form of a header + subsequent events (Server Side Events)
            request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));

            // Send request. Only need the header first
            HttpClient httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(5);

            HttpResponseMessage? result = null;
           
            try
            {
                result = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            }
            catch (Exception e)
            {
                if (onError != null)
                {
                    onError(e.Message);
                }
                return new Message() { role = "Failed", content = e.Message };

            }

            if (result == null)
            {
                return new Message() { role = "Failed", content = "" };
            }

            if (!result.IsSuccessStatusCode)
            {
                if (onError != null && result.ReasonPhrase != null)
                {
                    onError(result.ReasonPhrase);
                }
                return new Message() { role = "Failed", content = result.ReasonPhrase != null ? result.ReasonPhrase : "" };
            }

            // we got a header, now wait for a stream of server events. Roles and tokens will be
            // received individually.
            using (var stream = await result.Content.ReadAsStreamAsync())
            {
                using (var reader = new StreamReader(stream))
                {
                    while (!reader.EndOfStream)
                    {
                        string? line = await reader.ReadLineAsync();

                        if (line == null || !line.StartsWith("data: "))
                        {
                            continue;
                        }

                        string data = line.Substring("data: ".Length).Trim();

                        // last event?
                        if (data == "[DONE]")
                        {
                            continue;
                        }

                        try
                        {
                            CompletionResponse? response = JsonSerializer.Deserialize<CompletionResponse>(data);

                            if (response == null)
                            {
                                continue;
                            }

                            if (response.choices != null && response.choices.Count > 0)
                            {
                                Message msg = response.choices[0].message != null ? response.choices[0].message : response.choices[0].delta;

                                if (msg == null)
                                {
                                    continue;
                                }

                                // switch role?
                                if (msg.role != null && onRoleChanged != null)
                                {
                                    onRoleChanged(msg.role);
                                }

                                // new token?
                                if (msg.content != null && onToken != null)
                                {
                                    onToken(msg.content);
                                }

                                // append token to message content
                                responseMessage.content += msg.content;
                            }
                        }
                        catch (Exception ex)
                        {
                            if (onError != null)
                            {
                                onError(ex.ToString());
                            }

                            return new Message() { role = "exception", content = ex.ToString() };
                        }
                    }
                }
            }

            // add both user and assistant messages to history
            this.History.Add(userMessage);
            this.History.Add(responseMessage);

            return responseMessage;
        }
    }
}