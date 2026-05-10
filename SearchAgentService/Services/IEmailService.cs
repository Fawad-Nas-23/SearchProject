namespace SearchAgentService.Services
{
    public interface IEmailService
    {
        Task SendAgentResultAsync(string toEmail, string[] searchWords, int hits, List<string> documentUrls);

    }
}
