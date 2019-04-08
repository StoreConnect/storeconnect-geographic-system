using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace geoapi.Controllers
{
    [Route("api/reverse")]
    [ApiController]
    public class ReverseController : ControllerBase
    {
        IESHttpClient _client;

        public ReverseController(IESHttpClient client)
        {
            _client = client;
        }

        // GET api/reverse/<venueid>
        [HttpGet("{venueid}")]
        public async Task<ContentResult> Get(int venueid, double lon, double lat)
        {
            string url = "http://localhost:9200/venue_" + venueid + "/_search";

            JArray coords = new JArray();
            coords.Add(lon);
            coords.Add(lat);

            JObject doc = new JObject(
                new JProperty("query",
                    new JObject(
                        new JProperty("bool",
                            new JObject(
                                new JProperty("must", new JArray()),
                                new JProperty("filter",
                                    new JObject(
                                        new JProperty("geo_shape",
                                            new JObject(
                                                new JProperty("feature.geometry",
                                                    new JObject(
                                                        new JProperty("relation", "intersects"),
                                                        new JProperty("shape",
                                                            new JObject(
                                                                new JProperty("type", "point"),
                                                                new JProperty("coordinates", coords)
                                                            )
                                                        )
                                                    )
                                                )
                                            )
                                        )
                                    )
                                )
                            )
                        )
                    )
                )
            );

            var response = await _client.SearchDocument(url, doc.ToString());
            var result = await response.Content.ReadAsStringAsync();

            ContentResult r = new ContentResult();
            r.Content = result;
            r.ContentType = "application/json";
            r.StatusCode = (int)response.StatusCode;

            return r;
        }

    }
}
