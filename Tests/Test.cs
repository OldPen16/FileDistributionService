using NUnit.Framework;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using FileDistributionService;

namespace Tests
{
    [TestFixture]
    public class Test
    {
        private WebApplicationFactory<FileService> _webApplicationFactory;
        private HttpClient _httpClient;

        
        [SetUp]
        public void Setup()
        {
            _webApplicationFactory = new WebApplicationFactory<FileService>();
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://localhost:7034");
        }

                [Test]
        public async Task UploadFile()
        {
            // Create a sample file to upload
            var fileContent = new ByteArrayContent(new byte[] { 00000000000 });
            fileContent.Headers.Add("Content-Type", "application/octet-stream");

            var formData = new MultipartFormDataContent();
            formData.Add(fileContent, "file", "samplefile.txt");

            _httpClient.DefaultRequestHeaders.Add("x-api-key", "sada238238asdkkedm2349949565949");
            var response = await _httpClient.PostAsync("/files/upload", formData);
            
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [Test]
        public async Task DownloadFile()
        {
            _httpClient.DefaultRequestHeaders.Add("x-api-key", "sada238238asdkkedm2349949565949");
            var response = await _httpClient.GetAsync("/files/download?id=dc99a118-28db-4933-9b7e-ba3dfe180ef2_dummy7.txt");

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [Test]
        public async Task GetDashboardData()
        {
            _httpClient.DefaultRequestHeaders.Add("x-api-key", "sada238238asdkkedm2349949565949");
            var response = await _httpClient.GetAsync("/files/dashboarddata");

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [TearDown]
        public void TearDown()
        {
            _webApplicationFactory.Dispose();
            _httpClient.Dispose();
        }

    }
}