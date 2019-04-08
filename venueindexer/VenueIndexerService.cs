using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
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
        CancellationTokenSource _cts;
        Task _executingTask;

        public VenueIndexerService(IESHttpClient client, IConfiguration config)
        {
            _config = config;
            _client = client;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _source = _config["source"];
            _eshost = _config["eshost"];

            if (string.IsNullOrEmpty(_source) || string.IsNullOrEmpty(_eshost))
            {
                Console.WriteLine("USAGE : dotnet run venueindexer.dll file.json hosturl");
                Console.WriteLine("note - File must be in geojson format");
                return Task.CompletedTask;
            }

            Console.WriteLine("Starting");

            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _executingTask = Process(_cts.Token);

            return _executingTask.IsCompleted ? _executingTask : Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_executingTask == null)
            {
                return;
            }
            _cts.Cancel();
            cancellationToken.ThrowIfCancellationRequested();

            Console.WriteLine("Finished");
        }

        public async Task Process(CancellationToken cancellationToken)
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

                if (cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine("Execution cancelled");
                    break;
                }

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
