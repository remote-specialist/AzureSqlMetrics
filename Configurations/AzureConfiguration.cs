using Configurations.Models;
using Microsoft.Extensions.Configuration;

namespace Configurations
{
    public class AzureConfiguration : IAzureConfiguration
    {
        private readonly IConfiguration _configuration;
        public AzureConfiguration(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public AzureConfigurationModel GetConfiguration()
        {
            return _configuration.GetSection("AzureConfiguration").Get<AzureConfigurationModel>();
        }
    }
}