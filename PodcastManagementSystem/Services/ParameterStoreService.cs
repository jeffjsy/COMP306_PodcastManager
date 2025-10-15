using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using PodcastManagementSystem.Interfaces;
using System.Threading.Tasks;

namespace PodcastManagementSystem.Services
{
    public class ParameterStoreService : IParameterStoreService
    {
        private readonly IAmazonSimpleSystemsManagement _ssmClient;

        public ParameterStoreService(IAmazonSimpleSystemsManagement ssmClient)
        {
            _ssmClient = ssmClient;
        }

        public async Task<string> GetParameterAsync(string parameterName)
        {
            var request = new GetParameterRequest
            {
                Name = parameterName,
                WithDecryption = true // Crucial for retrieving encrypted credentials
            };

            try
            {
                var response = await _ssmClient.GetParameterAsync(request);
                return response.Parameter.Value;
            }
            catch (ParameterNotFoundException)
            {
                // Handle error if parameter isn't found (important for deployment)
                return null;
            }
        }
    }
}
