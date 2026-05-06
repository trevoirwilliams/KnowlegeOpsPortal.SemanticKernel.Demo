using System;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using KnowledgeOps.AI.Services;
using Microsoft.SemanticKernel;

namespace KnowledgeOps.AI.Plugins;

public class BusinessRequestPlugin(IBusinessRequestRepository requestRepository)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };
    
    [KernelFunction("get_request_by_id")]
    [Description("Retrieves the full details of a single KnowledgeOps business request when the user provides a specific request ID. Use this for questions about one known request, including its status, department, requester, justification, impact, urgency, due date, or assignment. Returns a JSON payload with a Found flag and either the matching request details or a not-found message.")]
    public async Task<string> GetRequestByIdAsync(
        [Description("The exact KnowledgeOps request ID to look up. The expected format is REQ followed by a hyphen and four digits, for example REQ-1001.")] string requestId)
    {
        var request = await requestRepository.GetByIdAsync(requestId);
        if (request is null)
        {
            return JsonSerializer.Serialize(new
            {
                Found = false,
                Message = $"No business request was found with ID '{requestId}'."
            }, JsonOptions);
        }

        return JsonSerializer.Serialize(new
        {
            Found = true,
            Request = request
        }, JsonOptions);
    }    

    [KernelFunction("get_open_business_requests")]
    [Description("Retrieves a list of all open KnowledgeOps business requests. Use this for questions about multiple requests that are currently open, including their status, department, requester, justification, impact, urgency, due date, or assignment. Returns a JSON payload with the count of open requests and the list of request details.")]
    public async Task<string> GetOpenRequestsAsync()
    {
        var requests = await requestRepository.GetOpenRequestsAsync();
        return JsonSerializer.Serialize(new
        {
            requests.Count,
            Requests = requests
        }, JsonOptions);
    }   
}
