using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace venueindexer
{
    public interface IVenueIndexerService
    {
        Task Process(String filename, String targethost);
    }


    public class VenueIndexerService : IHostedService
    {
        IConfiguration _config;
        IESHttpClient _client;
        String _eshost;
        String _source;
        Stopwatch _stopWatch;

        public VenueIndexerService(IESHttpClient client, IConfiguration config)
        {
            _config = config;
            _client = client;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _source = _config["source"];
            _eshost = _config["eshost"];

            if (string.IsNullOrEmpty(_source) || string.IsNullOrEmpty(_eshost))
            {
                Console.WriteLine("USAGE : dotnet venueindexer.dll source=file.json eshost=esurlwithindex");
                Console.WriteLine("note - File must be in geojson format");
                return;
            }

            _stopWatch = Stopwatch.StartNew();

            Console.WriteLine("Creating index");

            // Create collection
            var resp1 = await _client.PutDocument(_eshost, "");

            Console.WriteLine("Set mapping");

            // Set mapping
            JObject mapping = new JObject();
            mapping.Add(
                new JProperty("properties",
                    new JObject(
                        new JProperty("feature",
                            new JObject(
                                new JProperty("properties",
                                    new JObject(
                                        new JProperty("geometry",
                                            new JObject(
                                                new JProperty("type", "geo_shape")
                                            )
                                        ),
                                        new JProperty("props",
                                            new JObject(
                                                new JProperty("type", "object")
                                            )
                                        )
                                    )
                                )
                            )
                        )
                    )
                )
            );

            var resp2 = await _client.PutDocument(_eshost + "/_mappings/_doc", mapping.ToString());

            // Import data
            await Process();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("Finished in {0} s", _stopWatch.Elapsed.Seconds);
        }

        public async Task Process()
        {
            if (!File.Exists(_source))
            {
                Console.WriteLine(_source + " not found !");
                return;
            }

            // Open file
            StreamReader sr = new StreamReader(_source);
            String geojson = sr.ReadToEnd();

            // Get feature
            JObject obj = JObject.Parse(geojson);

            // For each feature
            int index = 0;
            JArray features = (JArray)obj["features"];
            foreach (JObject feature in features)
            {
                index++;

                Console.WriteLine(index + " / " + features.Count);

                String id = (String)feature["id"];

                // Keep only Polygons
                JObject geometry = (JObject)feature["geometry"];
                String type = (String)geometry["type"];
                if (type != "Polygon")
                {
                    continue;
                }

                // Create json doc
                JObject doc = new JObject();
                doc["feature"] = new JObject();
                doc["feature"]["geometry"] = geometry;
                doc["feature"]["props"] = feature["properties"];

                // Fix invalid coordinates
                JArray newrings = new JArray();
                foreach (JArray ring in geometry["coordinates"])
                {
                    JArray newring = new JArray();
                    JArray prevcoord = null;
                    int i = 0;
                    foreach(JArray coord in ring)
                    {
                        if(i > 0)
                        {
                            double prevlon = (double)prevcoord[0], prevlat = (double)prevcoord[1];
                            double lon = (double)coord[0], lat = (double)coord[1];
                            if(lon == prevlon && lat == prevlat)
                            {
                                Console.WriteLine("duplicate coords" + coord);
                            }
                            else
                            {
                                newring.Add(coord);
                            }
                        }
                        else
                        {
                            newring.Add(coord);
                        }
                        prevcoord = coord;
                        i++;
                    }
                    newrings.Add(newring);
                }

                doc["feature"]["geometry"]["coordinates"] = newrings;

                String docid = id.Replace("/", "_");
                String indexurl = _eshost + "/_doc/" + docid;

                var response = await _client.PutDocument(indexurl, doc.ToString());

                if(response.StatusCode == HttpStatusCode.Created ||
                    response.StatusCode == HttpStatusCode.OK)
                {
                   // Console.WriteLine(docid + " indexed");
                }
                else
                {
                    Console.WriteLine("ERROR indexing " + docid + " " + response.Content.ReadAsStringAsync().Result);
                }

            }

        }

    }

}
