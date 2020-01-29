
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SSMVCCoreApp.Controllers
{
    public class AboutController : Controller
    {
        private readonly IConfiguration _configuration;

        public AboutController(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public async Task<IActionResult> Index()
        {
            AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider();
            KeyVaultClient keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));

            var secrets = await keyVaultClient.GetSecretsAsync(_configuration["SportsStoreKeyVault"]);

            Dictionary<string, string> secretVauleList = new Dictionary<string, string>();
            foreach (var item in secrets)
            {
                var secret = await keyVaultClient.GetSecretAsync(item.Id);
                secretVauleList.Add(item.Id, secret.Value);
            }
            return View(secretVauleList);
        }
        public IActionResult Throw()
        {
            throw new EntryPointNotFoundException("This is a user thrown exception");
        }
    }
}
