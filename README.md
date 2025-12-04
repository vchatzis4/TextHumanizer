# TextHumanizer

A web application that humanizes AI-generated text and detects AI-written content.

## Features

- **Humanize Text** - Transform robotic AI-generated text into natural, human-sounding prose
- **Detect AI Text** - Analyze text to determine the probability it was written by AI

Supports **English** and **Greek** languages.

## Tech Stack

- **Backend:** ASP.NET Core 8 Web API
- **Frontend:** Blazor WebAssembly
- **LLM Provider:** Groq (llama-3.3-70b-versatile)

## Live Demo

- **App:** https://texthumanizer.up.railway.app
- **API:** https://texthumanizerapi-production.up.railway.app

## Local Development

### Prerequisites

- .NET 8 SDK
- Groq API key ([get one here](https://console.groq.com))

### Setup

1. Clone the repository
   ```bash
   git clone https://github.com/vchatzis4/TextHumanizer.git
   cd TextHumanizer
   ```

2. Copy the template config
   ```bash
   cp appsettings.template.json appsettings.json
   ```

3. Add your Groq API key to `appsettings.json`

4. Run the API
   ```bash
   dotnet run
   ```

5. Run the UI (in a separate terminal)
   ```bash
   cd TextHumanizer.UI
   dotnet run
   ```

6. Open http://localhost:5054 (API) and the Blazor UI port

## Deployment

Deployed on Railway with Docker. See `Dockerfile` and `TextHumanizer.UI/Dockerfile`.

Environment variables needed:
- `ASPNETCORE_ENVIRONMENT=Production`
- `GROQ_API_KEY=your_api_key`

## License

MIT
