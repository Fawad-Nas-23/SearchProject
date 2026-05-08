using System;
namespace Shared.Model;

public class SearchAgentRequest
{
    public string Email { get; set; } = "";
    public string[] SearchWords { get; set; } = Array.Empty<string>();
}