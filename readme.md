# SemanticIntel

A set of services which enables you to index and explore data related to configured services while using Generative AI.

This repository aims to act as an example both for exploration, documentation and opportunities to mix new technologies together. In the long run this may evolve to something else and/or be abondon for something else.

## Running the application

Before you can start using this application you need to make sure you fulfill a set of prerequisite.

### Prerequisites

- Having latest [.Net 8 SDK](https://dotnet.microsoft.com/en-us/download) installed (with Dotnet CLI)
- Having [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli) installed and being logged in to a subscription which has an OpenAI service deployed
- A [Qdrant](https://qdrant.tech/) instance running (recommended: using a local docker container instance for testing)
- Configuring a set of user-secrets (see example below for instructions)

Even if it's not a required prerequisite, it is highly recommended to install docker. This project will probably pivot to start using Aspire and will rely on some containers to store and handle data/services/queues in the future.


### Setting user-secrets

Currently there are 7 secrets (or environment variables/appsettings) you need to have configured. Easiest way of settings these up are using dotnet cli and user-secrets, see example below:

```bash
# Textgeneration endpoint + deployment
dotnet user-secret set "SemanticKernel:TextGeneration:Endpoint" "<endpoint here>"
dotnet user-secret set "SemanticKernel:TextGeneration:Deployment" "<deployment here>"

# Embedding endpoint + deployment
dotnet user-secret set "SemanticKernel:Embedding:Endpoint" "<endpoint here>"
dotnet user-secret set "SemanticKernel:Embedding:Deployment" "<deployment here>"

# ChatCompletion endpoint + deployment
dotnet user-secret set "SemanticKernel:ChatCompletion:Endpoint" "<endpoint here>"
dotnet user-secret set "SemanticKernel:ChatCompletion:Deployment" "<deployment here>"

# Qdrant endpoint
dotnet user-secret set "SemanticKernel:Qdrant:Endpoint" "http://localhost:6333"
```

### Setting up a Qdrant instance

Easiest way of setting up a local instance of Qdrant is to use a docker container. In order to run one you can run the following command:

```bash
docker run --rm -p 6333:6333 qdrant/qdrant
```

### Running the application

If you're using a terminal, all you need to do is navigate to ./src/services/SemanticIntel.Services.Api and then run `dotnet run`.

If you're using VS Code and/or Visual Studio (or similar), all you need to do is making sure to run the `SemanticIntel.Services.Api` project.
