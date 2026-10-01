using System.Net;
using Xunit;

namespace PetCareAI.Tests.Integration
{
    public class PetControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public PetControllerIntegrationTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetHealth_ComApiEmExecucao_DeveRetornarOkEHealthy()
        {
            // Arrange
            var url = "/health";

            // Act
            var response = await _client.GetAsync(url);
            var body = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Healthy", body);
        }
    }
}
