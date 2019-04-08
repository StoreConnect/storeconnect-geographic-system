using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace venueindexer
{
    public interface IESHttpClient
    {
        Task<HttpResponseMessage> PutDocument(string url, string document);
    }

    public class ESHttpClient : IESHttpClient
    {
        private readonly HttpClient _client;

        public ESHttpClient(HttpClient client)
        {
            _client = client;
        }

        public async Task<HttpResponseMessage> PutDocument(string url, string document)
        {
            HttpContent content = new StringContent(document, Encoding.UTF8, "application/json");
            var response = await _client.PutAsync(url, content);
            return response;
        }
    }
}
