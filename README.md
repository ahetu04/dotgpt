# `gpta`

`gpta` is a .NET 8 console app that gives you a fast, streaming ChatGPT-style assistant directly in your terminal. You launch the binary once, type prompts conversationally, and steer the conversation with lightweight slash commands. Assistants, sessions, settings, and chat transcripts live under `%LOCALAPPDATA%/gpta`, so you can pick up where you left off every time you reopen the app.

## Key features
- **Streaming replies** – tokens arrive as soon as OpenAI sends them, so you can read answers while they are being generated.
- **Persistent assistants** – each assistant remembers its model, instructions, and history window; `/assistant <name>` swaps between them instantly.
- **Session management** – `/session <name>` keeps parallel conversations, and `/clear` or `/reset` let you start fresh.
- **Markdown export** – `/savemd <file>` writes the current session to `%LOCALAPPDATA%/gpta/Saved/<file>.md`.
- **Local-only state** – API keys, assistants, sessions, and saved chats never leave your machine; the only network call is the OpenAI Chat Completions request.

## Build & run
```pwsh
dotnet restore
dotnet run --project cl-gpta        # launches the interactive prompt

# or publish a standalone build (example: win-x64)
dotnet publish cl-gpta -c Release -r win-x64 --self-contained false
```

On first launch, `gpta` prompts you for an OpenAI API key (a paid account key from https://platform.openai.com/account/api-keys). The key is cached in `%LOCALAPPDATA%/gpta/settings.json`, and you can rotate it later with `/key <new-key>`.

## Interactive workflow
1. Start the app (`gpta` after publishing, or `dotnet run --project cl-gpta` while developing).
2. Type prompts normally. Replies stream back with colored role prefixes.
3. Use slash commands whenever you need to configure the assistant or session (listed below).
4. Type `exit`, `quit`, or `q` (with or without a leading `/`) to leave the prompt.

### Slash commands

| Command | Description |
|---------|-------------|
| `/key <api-key>` | Updates the stored OpenAI API key and applies it to the current session. |
| `/assistant <name>` | Switches (or creates) an assistant profile with its own instructions, model, and history length. |
| `/session <name>` | Loads or creates a chat session so you can maintain multiple ongoing conversations. |
| `/instructions "<text>"` | Replaces the system prompt for the active assistant/session. Quotes are optional; use them for multi-line text. |
| `/model <model-name>` | Sets the OpenAI Chat Completions model (defaults to `gpt-5.1`). |
| `/history <n>` | Controls how many previous messages accompany each new prompt (per assistant). |
| `/clear` | Empties the current session’s chat history. |
| `/reset` | Reverts to the default assistant and session and wipes their history. |
| `/status` | Lists every assistant/session stored under `%LOCALAPPDATA%/gpta`, highlighting the active pair. |
| `/savemd <filename>` | Exports the current session to Markdown under `%LOCALAPPDATA%/gpta/Saved/`. |
| `/help` | Prints an in-app summary of the available commands. |

Any other input is treated as a prompt. Commands are only recognized when the line starts with `/`.

## Storage layout

```
%LOCALAPPDATA%/gpta/
|-- settings.json              # global API key + default assistant/session names
|-- Assistants/<name>.json     # serialized assistant profiles
|-- Sessions/<name>.json       # serialized chat history for each session
`-- Saved/<export>.md          # optional Markdown exports created via /savemd
```

Deleting any of these files is safe; they will be recreated on demand. Keep in mind that history and instructions live with their respective assistant or session files.

## `dotgpt.OpenAI.Chat.Session`

The `dotgpt` class library ships with a reusable `dotgpt.OpenAI.Chat.Session` type that handles history management, streaming responses, and persistence. `cl-gpta` uses it directly, but you can also drop it into your own .NET projects.

```csharp
string apiKey = "{YOUR-API-KEY}";
var session = new dotgpt.OpenAI.Chat.Session(apiKey)
{
    Name = "scratch",
    Instructions = "You are an AI assistant who answers concisely.",
    Model = "gpt-5.1",
    PromptHistory = 5,
};

var onRoleChanged = (string role) => Console.Write($"\n{role} > ");
var onToken = (string token) => Console.Write(token);
var onError = (string error) => Console.WriteLine($"\nError: {error}");

await session.EnterPrompt("What's a detached HEAD in git?", onRoleChanged, onToken, onError);
session.Save();   // persists under %LOCALAPPDATA%/gpta/Sessions/
```

## OpenAI API key reminder

You need an API key tied to a paid OpenAI account to talk to the Chat Completions API. Create or manage keys at https://platform.openai.com/account/api-keys, then provide the key to `gpta` when prompted or via `/key <value>`.
