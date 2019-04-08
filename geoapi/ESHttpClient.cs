using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace geoapi
{

    public interface IESHttpClient
    {
        Task<HttpResponseMessage> SearchDocument(string url, string document);
    }

    public class ESHttpClient : IESHttpClient
    {
        private readonly HttpClient _client;

        public ESHttpClient(HttpClient client)
        {
            _client = client;
        }

        public async Task<HttpResponseMessage> SearchDocument(string url, string body)
        {
            HttpContent content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await _client.PostAsync(url, content);
            return response;            
        }
    }

}
