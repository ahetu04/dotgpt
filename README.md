# `gpta`

Is a ChatGPT-style assistant that can be easily accessed from the terminal. Users have the option to either use the default assistant or configure their own. Assistants and sessions are persistent to ensure continuity between uses. 

While `gpta` communicates with OpenAI's chat API, all assistants and chat sessions are kept exclusively on the user's computer. `gpta` will prompt you to enter a valid OpenAI API key once.

Simply put, with `gpta` you can easily access and use ChatGPT without any hassle. 

## command line arguments
Arguments are all optional. Their uses range from setting a new API key to creating and modifying assistant profiles.  
- `key` : Sets the API key linked to your OpenAI account.
- `assistant` : Switches the current assistant. If the assistant doesn't exist, it is created. Default is 'default'.
- `instructions` : These are the instructions that your assistant will follow for each prompt you send. Default is "You are a helpful AI assistant. Answer as concisely as possible.".
- `model` : Allows you to try out new models in the future, assuming that the chat API will remain the same. Default is 'gpt-3.5-turbo'. 
- `temp` : Tweaks the temperature. Default is 0.5.
- `tokens` : Sets the maximum number of tokens allowed for the answer. Default is 1024.
- `history` : Sets the number of previous messages that should be resent with each prompt. Default is 5.
- `session` : Creates and/or switch to a new chat session. The default session name is 'default'.
- `reset` : Sets the current assistant to 'default' and resets its settings. Also sets the session to default and clears its history. 
- `lists` : Lists all assistants and sessions available. Also lists the current assistant's settings.
- `help` : Prints information about the command line arguments. 

Example:

    ./gpta -key:"YOUR-OPENAI-KEY" -assistant:"git expert" -instructions:"You are an AI assistant good at solving problems with Git" -model:gpt-3.5-turbo -temp:0.6 -tokens:1500 -history:4 -session:git -reset -lists -help
    
    To change your API key:
    ./gpta -key:"Your new key"
    
    To change assistant:
    ./gpta -assistant:FrenchTranslator

    To change the instructions for the current assistant:
    ./gpta -instructions:"You are a French translator. Translate ..."

    To switch back to a default assistant and a fresh session:
    ./gpta -reset

    To continue where you left off before:
    ./gpta

When in the prompt, type 'exit', 'quit' or 'q' to leave.  

# `dotgpt.OpenAI.Chat.Session`
This is a straightforward C# class that facilitates communication with Open AI's chat completion API. The class leverages an event stream to efficiently receive tokens as they get generated.

## How to use in your
```CSharp
    string apiKey = "{YOUR-API-KEY}";
    dotgpt.OpenAI.Chat.Session session = new dotgpt.OpenAI.Chat.Session(apiKey)
    {
        PromptHistory = 5,
        Instructions = "You're an AI assistant capable of providing detailed answers to technical questions. ",
        MaxTokens = 1024
    };

    string prompt = "What's a detached HEAD in git?";

    var onRoleChanged = (string role) => { Console.Write($"\n{role}:\n"); };
    var onToken = (string token) => { Console.Write(token); };
    dotgpt.OpenAI.Chat.Message m = await session.EnterPrompt(prompt, onRoleChanged, onToken);

```

# `OpenAI API key`
To access the OpenAI Chat API you'll need an API key linked to a OpenAI paid account. Visit https://platform.openai.com/account/api-keys to 
generate/obtain your API key(s). 
