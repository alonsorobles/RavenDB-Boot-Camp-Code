using System;
using System.IO;
using System.Linq;
using System.Net;
using Newtonsoft.Json;
using Raven.Abstractions.Commands;
using Raven.Client;
using Raven.Client.Document;
using Raven.Json.Linq;

namespace NugetImport
{
    class Program
    {
        static void Main()
        {
            using (var store = new DocumentStore {Url = "http://localhost:8080", DefaultDatabase = "Nuget"}.Initialize())
            {
                string url = "https://nuget.org/api/v2/Packages";
                while(true)
                {
                    Console.Out.WriteLine("Get {0}", url);
                    if (url == null)
                        break;
                    var webRequest = (HttpWebRequest) WebRequest.Create(url);
                    webRequest.Accept = "application/json";
                    using (var response = webRequest.GetResponse())
                    using (var stream = response.GetResponseStream())
                        url = WritePackagesToRaven(stream, store);
                }
            }
        }

        private static string WritePackagesToRaven(Stream stream, IDocumentStore store)
        {
            var json = RavenJToken.ReadFrom(new JsonTextReader(new StreamReader(stream))).Value<RavenJObject>("d");
            using (var session = store.OpenSession())
            {
                foreach (RavenJObject result in json.Value<RavenJArray>("results"))
                {
                    ModifyResult(result);
                    session.Advanced.Defer(new PutCommandData
                        {
                            Document = result,
                            Metadata = new RavenJObject{{"Raven-Entity-Name", "Packages"}},
                            Key = "packages/" + result.Value<string>("Id") + "/" + result.Value<string>("Version")
                        });
                }
                session.SaveChanges();
            }
            return json.Value<string>("__next");
        }

        private static void ModifyResult(RavenJObject result)
        {
            var tags = result.Value<string>("Tags");
            if (tags != null)
                result["Tags"] =
                    new RavenJArray(tags.Split(new[] {' ', ',', ';'}, StringSplitOptions.RemoveEmptyEntries));
            else
                result["Tags"] = new RavenJArray();
            var dependencies = result.Value<string>("Dependencies");
            if (dependencies != null)
                result["Dependencies"] =
                    new RavenJArray(dependencies.Split(new[] {'|'}, StringSplitOptions.RemoveEmptyEntries).Select(s =>
                        {
                            var strings = s.Split(':');
                            return RavenJObject.FromObject(new {Package = strings[0], Version = strings[1]});
                        }));
            result.Remove("__metadata");
        }
    }
}
