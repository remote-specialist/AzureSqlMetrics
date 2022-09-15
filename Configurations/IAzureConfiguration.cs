using Configurations.Models;

namespace Configurations
{
    public interface IAzureConfiguration
    {
        AzureConfigurationModel GetConfiguration();
    }
}
