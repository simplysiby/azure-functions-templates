// Setup
// 1) Go to https://www.microsoft.com/cognitive-services/en-us/computer-vision-api 
//    Sign up for computer vision api
// 2) Go to Platform features -> Application settings
//    create a new app setting Vision_API_Subscription_Key and use Computer vision key as value

#if (portalTemplates)
#r "Microsoft.WindowsAzure.Storage"
#r "Newtonsoft.Json"

using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage.Table;

public static async Task Run(Stream image, string name, IAsyncCollector<FaceRectangle> outTable, TraceWriter log)
#endif
#if (vsTemplates)
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace Company.Function
{
    public static class FaceLocatorCSharp
    {
        private static HttpClient client = new HttpClient();
        [FunctionName("FaceLocatorCSharp")]
        public static async Task Run([BlobTrigger("BlobPathValue", Connection = "BlobConnectionValue")]Stream image, string name, [Table("TableNameValue", Connection = "TableConnectionValue")]IAsyncCollector<FaceRectangle> outTable, TraceWriter log)
#endif
        {
            string result = await CallVisionAPI(image);
            log.Info(result);

            if (String.IsNullOrEmpty(result))
            {
                return;
            }

            ImageData imageData = JsonConvert.DeserializeObject<ImageData>(result);
            foreach (Face face in imageData.Faces)
            {
                var faceRectangle = face.FaceRectangle;
                faceRectangle.RowKey = Guid.NewGuid().ToString();
                faceRectangle.PartitionKey = "Functions";
                faceRectangle.ImageFile = name + ".jpg";
                await outTable.AddAsync(faceRectangle);
            }
        }

        static async Task<string> CallVisionAPI(Stream image)
        {
            var content = new StreamContent(image);
            var url = "https://westus.api.cognitive.microsoft.com/vision/v1.0/analyze?visualFeatures=Faces&language=en";
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", Environment.GetEnvironmentVariable("Vision_API_Subscription_Key"));
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            var httpResponse = await client.PostAsync(url, content);

            if (httpResponse.StatusCode == HttpStatusCode.OK)
            {
                return await httpResponse.Content.ReadAsStringAsync();
            }
            else
            {
                throw new HttpRequestException(await httpResponse.Content.ReadAsStringAsync());
            }
        }

        public class ImageData
        {
            public List<Face> Faces { get; set; }
        }

        public class Face
        {
            public int Age { get; set; }

            public string Gender { get; set; }

            public FaceRectangle FaceRectangle { get; set; }
        }

        public class FaceRectangle : TableEntity
        {
            public string ImageFile { get; set; }

            public int Left { get; set; }

            public int Top { get; set; }

            public int Width { get; set; }

            public int Height { get; set; }
        }
#if (vsTemplates)
    }
}
#endif