# LocalLLM Studio

LocalLLM Studio is a web-based application that allows you to train, register, and chat with your own local large language models. It provides a user-friendly interface for managing your models and datasets, and for interacting with your models in a chat-like interface.

## Project Structure

The project is divided into two main parts: a .NET Core backend and a React frontend.

- `LocalLLM.API/`: The .NET Core backend, which provides a RESTful API for managing models, datasets, and training jobs.
- `LocalLLM.React.Chat/`: The React frontend, which provides a web-based interface for interacting with the backend.
- `data1.jsonl`, `data2.jsonl`: Sample data files for training.
- `Modelfile`: A sample Modelfile for Ollama.
- `requirements.txt`: The Python requirements for the training script.

### `LocalLLM.API/`

- `Controllers/`: Contains the API controllers for handling HTTP requests.
- `Contracts/`: Contains the data transfer objects (DTOs) for the API.
- `Services/`: Contains the business logic for the application.
- `train.py`: The Python script for training the models.
- `appsettings.json`: The configuration file for the application.
- `Program.cs`: The main entry point for the application.

### `LocalLLM.React.Chat/`

- `src/`: The source code for the React application.
- `components/`: Contains the React components for the application.
- `lib/`: Contains the API client for interacting with the backend.
- `App.tsx`: The main application component.
- `main.tsx`: The main entry point for the application.

## Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js](https://nodejs.org/)
- [Python](https://www.python.org/)
- [Ollama](https://ollama.ai/)

### Installation

1. Clone the repository.
2. Install the backend dependencies:
   ```bash
   dotnet restore LocalLLM.API
   ```
3. Install the frontend dependencies:
   ```bash
   npm install --prefix LocalLLM.React.Chat
   ```
4. Install the Python dependencies:
   ```bash
   pip install -r requirements.txt
   ```
5. Install the Llama3 model using ollama:
   ```bash
   ollama pull llama3:8b
   ```

### Running the Application

1. In a separate terminal, run the backend server:
   ```bash
   dotnet run --project LocalLLM.API
   ```
2. In another separate terminal, run the frontend development server:
   ```bash
   npm run dev --prefix LocalLLM.React.Chat
   ```

## Usage

1. Open your web browser and navigate to `http://localhost:5173`.
2. You will see the chat interface. You can select a model from the dropdown menu and start chatting with it.
3. To train a new model, click on the "Train & Register" tab.
4. Upload a dataset in JSONL format.
5. Select a base model and configure the training parameters.
6. Click on the "Launch" button to start the training job.
7. Once the training is complete, you can register the model by giving it a name and clicking on the "Register" button.
8. The new model will be available in the chat interface.

## Sample Trained Model

The project includes a sample trained model. To use it, run the following commands:

```bash
cd LocalLLM.API\runs\20250828194529736\artifacts
ollama create trained-model -f Modelfile
```

## Dependencies

### Backend

- OllamaSharp
- Microsoft.AspNetCore.OpenApi
- Swashbuckle.AspNetCore

### Frontend

- react
- react-dom

### Python

- torch
- transformers
- peft
- datasets
- PyYAML
- accelerate
- bitsandbytes
