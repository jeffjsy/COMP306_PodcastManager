namespace PodcastManagementSystem.Interfaces
{
    public interface IParameterStoreService
    {
        
        Task<string> GetParameterAsync(string parameterName);
    }
}
