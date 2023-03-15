using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SOAPBox.Model
{
    public static class GitHelper
    {
        //public static JObject CreateJObjectFromRESTData(string restData)
        //{
        //    var stream = CreateStreamFromString(restData);
        //    JsonSerializer serializer = JsonSerializer.Create(new JsonSerializerSettings());
        //    using (var streamReader = new StreamReader(stream, new UTF8Encoding(false, true)))
        //    {
        //        using (var jsonTextReader = new JsonTextReader(streamReader))
        //        {
        //            return (JObject)serializer.Deserialize(jsonTextReader);
        //        }
        //    }
        //}

        public static JObject CreateJObjectFromRESTData(string restData)
        {
            return JsonConvert.DeserializeObject<JObject>(restData);
        }

        //private static Stream CreateStreamFromString(string stringInput)
        //{
        //    var stream = new MemoryStream();
        //    var writer = new StreamWriter(stream);

        //    writer.Write(stringInput);
        //    writer.Flush();
        //    stream.Position = 0;
        //    return stream;
        //}

        public async static Task<string> GetInfoFromGitApiAsync(string pushUrl)
        {
            using (HttpMessageHandler messageHandler = CreateMessageHandler())
            using (HttpClient client = new HttpClient(messageHandler))
            {
                try
                {
                    return await client.GetStringAsync(pushUrl);                    
                }
                catch (Exception ex)
                {
                    throw new Exception(pushUrl, ex);
                }
            }
        }

        public async static Task<string> Post(string postUrl, string postContent)
        {
            using (HttpMessageHandler messageHandler = CreateMessageHandler())
            using (HttpClient client = new HttpClient(messageHandler))
            {
                try
                {
                    var response = await client.PostAsync(postUrl, JsonContent.Create(postContent));
                    return await response.Content.ReadAsStringAsync();
                }
                catch (Exception ex)
                {
                    throw new Exception(postUrl, ex);
                }
            }
        }

        private static HttpMessageHandler CreateMessageHandler()
        {
            return new HttpClientHandler()
            {
                UseDefaultCredentials = true,
                AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip,
                AllowAutoRedirect = true,
                CookieContainer = new CookieContainer()                
            };
        }

        public async static Task<JObject> GetDiffsFromApiAsync(string collectionUri, Guid repositoryId, string baseVersion, string targetVersion,
            string baseVersionType = "commit", string targetVersionType = "commit", int diffCount = 1000)
        {
            var commitsQueryString = $"{collectionUri}/_apis/git/repositories/{repositoryId}/diffs/commits?";
            var baseParameters = $"baseVersion={baseVersion}&baseVersionType={baseVersionType}";
            var targetParameters = $"&targetVersion={targetVersion}&targetVersionType={targetVersionType}";
            var extraParams = $"&diffCommonCommit=false&%24top={diffCount}&%24skip=0";
            var diffsResponse = await GetInfoFromGitApiAsync($"{commitsQueryString}{baseParameters}{targetParameters}{extraParams}");
            return CreateJObjectFromRESTData(diffsResponse);
        }

        public async static Task<string> GetPushesFromApiAsync(string collectionUri, string repositoryName, string pusherId)
        {
            
            var pushesQueryString = $"{collectionUri}/_apis/git/repositories/{repositoryName}/pushes?searchCriteria.pusherId={pusherId}&searchCriteria.includeRefUpdates=true";
            return await GetInfoFromGitApiAsync($"{pushesQueryString}");
            //return CreateJObjectFromRESTData(diffsResponse);
        }


    }
}
