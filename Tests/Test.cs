using NUnit.Framework;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;

namespace Tests
{
    [TestFixture]
    public class Test
    {
        private WebApplicationFactory<FileDistributionService.FileService> _webApplicationFactory;
        private HttpClient _httpClient;

        
        [SetUp]
        public void Setup()
        {
            _webApplicationFactory = new WebApplicationFactory<FileDistributionService.FileService>();
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

            var response = await _httpClient.PostAsync("/files/upload", formData);
            
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [Test]
        public async Task DownloadFile()
        {
            var response = await _httpClient.GetAsync("/files/download?id=9857aac1-c581-47e9-80c9-bc123f490d1d_dummy-image.png");

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