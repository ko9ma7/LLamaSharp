﻿namespace LLama.Examples.NewVersion
{
    using LLama.Common;
    using System;
    using System.Reflection;

    internal class CodingAssistant
    {
        const string DefaultModelUri = "https://huggingface.co/TheBloke/CodeLlama-7B-Instruct-GGML/resolve/main/codellama-7b-instruct.ggmlv3.Q4_K_S.bin";

        // Source paper with example prompts:
        // https://scontent-ham3-1.xx.fbcdn.net/v/t39.2365-6/369856151_1754812304950972_1159666448927483931_n.pdf?_nc_cat=107&ccb=1-7&_nc_sid=3c67a6&_nc_ohc=wURKmnWKaloAX9CL8rD&_nc_ht=scontent-ham3-1.xx&oh=00_AfBSvnWP6BkLgXzZ0OvLGkiDbkejxoM03Xg2ghVhn_InZQ&oe=64EEAC4F
        const string InstructionPrefix = "[INST]";
        const string InstructionSuffix = "[/INST]";
        const string SystemInstruction = "You're an intelligent, concise coding assistant. Wrap code in ``` for readability. Don't repeat yourself. Use best practice and good coding standards.";
        private static string ModelsDirectory = Path.Combine(Directory.GetParent(Assembly.GetExecutingAssembly().Location)!.FullName, "Models");

        public static async Task Run()
        {
            Console.Write("Please input your model path (if left empty, a default model will be downloaded for you): ");
            var modelPath = Console.ReadLine();

            if(string.IsNullOrWhiteSpace(modelPath) )
            {
                modelPath = await GetDefaultModel();
            }

            var parameters = new ModelParams(modelPath)
            {
                ContextSize = 4096,
                Seed = 1337,
                GpuLayerCount = 5
            };
            using var model = LLamaWeights.LoadFromFile(parameters);
            using var context = model.CreateContext(parameters);
            var executor = new InstructExecutor(context, InstructionPrefix, InstructionSuffix);

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("The executor has been enabled. In this example, the LLM will follow your instructions." +
                "It's a 7B Code Llama, so it's trained for programming tasks like \"Write a C# function reading a file name from a given URI\" or \"Write some programming interview questions\".");
            Console.ForegroundColor = ConsoleColor.White;

            var inferenceParams = new InferenceParams() { 
                Temperature = 0.8f, 
                MaxTokens = -1,
            };

            string instruction = $"{SystemInstruction}\n";
            await Console.Out.WriteAsync("Instruction: ");
            instruction += Console.ReadLine() ?? "Ask me for instructions.";
            while (true)
            {

                Console.ForegroundColor = ConsoleColor.Green;
                foreach (var text in executor.Infer(instruction+System.Environment.NewLine, inferenceParams))
                {
                    Console.Write(text);
                }
                Console.ForegroundColor = ConsoleColor.White;

                await Console.Out.WriteAsync("Instruction: ");
                instruction = Console.ReadLine() ?? "Ask me for instructions.";
            }
        }

        private static async Task<string> GetDefaultModel()
        {
            var uri = new Uri(DefaultModelUri);
            var modelName = uri.Segments[^1];
            await Console.Out.WriteLineAsync($"The following model will be used: {modelName}");
            var modelPath = Path.Combine(ModelsDirectory, modelName);
            if(!Directory.Exists(ModelsDirectory))
            {
                Directory.CreateDirectory(ModelsDirectory);
            }

            if (File.Exists(modelPath))
            {
                await Console.Out.WriteLineAsync($"Existing model found, using {modelPath}");
            }
            else
            {
                await Console.Out.WriteLineAsync($"Model not found locally, downloading {DefaultModelUri}...");
                using var http = new HttpClient();
                await using var downloadStream = await http.GetStreamAsync(uri);
                await using var fileStream = new FileStream(modelPath, FileMode.Create, FileAccess.Write);
                await downloadStream.CopyToAsync(fileStream);
                await Console.Out.WriteLineAsync($"Model downloaded and saved to {modelPath}");
            }


            return modelPath;
        }
    }
}
