using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.Identity.Client;
using Newtonsoft.Json;


////using OpenAI;
//using OpenAI.Chat;
class Program
{
    private static string AZURE_OPENAI_ACCOUNT = "<OPENAI ACCOUNT URL>";
    private static string AZURE_SEARCH_SERVICE = "<SEARCH ACCOUNT URL>";
    private static string AZURE_DEPLOYMENT_MODEL = "gpt-35-turbo";
    private static string INDEX_NAME = "hotels-sample-index";

    static async Task Main(string[] args)
    {
        // Set up Azure Identity and OpenAI client
        var credential = new DefaultAzureCredential();
          var openAIClient = new OpenAIClient(new Uri(AZURE_OPENAI_ACCOUNT), credential);
      
        var searchClient = new SearchClient(new Uri(AZURE_SEARCH_SERVICE), INDEX_NAME, credential);

        string query = "Can you recommend a few hotels near the ocean with beach access and good views?";
        string GROUNDED_PROMPT = $@"
        You are a friendly assistant that recommends hotels based on activities and amenities.
        Answer the query using only the sources provided below in a friendly and concise manner.
        Answer ONLY with the facts listed in the list of sources below.
        If there isn't enough information below, say you don't know.
        Do not generate answers that don't use the sources below.
        Query: {query}
        Sources:\n{{sources}}
        ";

        var searchOptions = new SearchOptions
        {
            Size = 5 // Limit the results to 5
        };

        searchOptions.Select.Add("Description");
        searchOptions.Select.Add("HotelName");
        searchOptions.Select.Add("Tags");


        // Retrieve the selected fields from the search index related to the question.
        var searchResults = await searchClient.SearchAsync<SearchResult>(query, new SearchOptions
        {
            Size = 5,
            Select = { "Description", "HotelName", "Tags" }
        });

        // Format the sources
        var sourcesFormatted = string.Join("\n", searchResults.Value.GetResults().Select(document =>
        {
            var hotelName = document.Document.HotelName;
            var description = document.Document.Description;
            var tags = document.Document.Tags;
            return $"{hotelName}: {description}: {tags}";
        }));
      

        var chatMessages = new List<ChatRequestMessage>
        {
          new ChatRequestUserMessage("Can you recommend a few hotels near the ocean with beach access?"),
        };

        // Create the chat completions options
        var chatCompletionsOptions = new ChatCompletionsOptions(AZURE_DEPLOYMENT_MODEL, chatMessages);
    
        try
        {
            Response<ChatCompletions> response = await openAIClient.GetChatCompletionsAsync(chatCompletionsOptions);
            string reply = response.Value.Choices[0].Message.Content; // Extract the content of the first choice
            Console.WriteLine("AI Response:");
            Console.WriteLine(reply);
        }
        catch (RequestFailedException ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}





//class Program
//{
//    // Replace with your Azure OpenAI account and deployment name
//    private static string AZURE_OPENAI_ACCOUNT = "https://<your-openai-account>.openai.azure.com/";
//    private static string AZURE_DEPLOYMENT_NAME = "<your-deployment-name>"; // The name of your Azure OpenAI deployment

//    static async Task Main(string[] args)
//    {
//        // Create a credential using Azure Identity
//        var credential = new DefaultAzureCredential();

//        // Initialize the Azure OpenAI client
//        var openAiClient = new OpenAIClient(new Uri(AZURE_OPENAI_ACCOUNT), credential);

//        // Prepare the messages for the chat
//        var chatMessages = new[]
//          {
//            new ChatMessage(ChatRole.User, "Hello! Can you recommend a few hotels near the ocean with beach access?")
//        };

//        // Create chat completions options
//        var chatCompletionsOptions = new ChatCompletionsOptions();
//        //{
//        //    Messages =  { chatMessages } // Correctly add messages to the options
//        //};

//        // Get chat completion from Azure OpenAI
//        try
//        {
//            Response<ChatCompletions> response = await openAiClient.GetChatCompletionsAsync(chatCompletionsOptions);
//            string reply = response.Value.Choices[0].Message.Content; // Extract the content of the first choice
//            Console.WriteLine("AI Response:");
//            Console.WriteLine(reply);
//        }
//        catch (RequestFailedException ex)
//        {
//            Console.WriteLine($"Error: {ex.Message}");
//        }
//    }
//}


public class SearchResult
{
    public string HotelName { get; set; }
    public string Description { get; set; }
    public List<string> Tags { get; set; }
}

public class ChatMessage
{
    public ChatRole Role { get; }
    public string Content { get; }

    public ChatMessage(ChatRole role, string content)
    {
        Role = role;
        Content = content;
    }
}

public enum ChatRole
{
    User,
    Assistant
}
