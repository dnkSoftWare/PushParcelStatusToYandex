using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using NLog;

namespace YandexPUSH
{
    class YandexAPI: HttpClient
    {
        private readonly ILogger _logger;
        public XmlDocument xml;
        private static string token = "PRjHI9Q9kvfH6thGY5ttBnzI05nw4gWpjYvHD7eb2oP4MsRBCzP4WNAVTlEqeK16";
        public YandexAPI(ILogger logger): base()
        {
            _logger = logger;
            xml = new XmlDocument();
            
            XmlNode docNode = xml.CreateXmlDeclaration("1.0", "UTF-8", null);
            xml.AppendChild(docNode);

        }

        private async Task<string> RequestPostAsync(string uri, string xmlData)
        {
            string resp;
                var content = new ByteArrayContent(Encoding.UTF8.GetBytes(xmlData));
                content.Headers.Add("Content-Type", "text/xml");
                var response = await PostAsync(uri, content);
                response.EnsureSuccessStatusCode();
                resp = await response.Content.ReadAsStringAsync();
            return resp;
        }



        public string PushOrdersStatusesChanged(Parcel  parcel)
        {
            var res = "";
            var root = xml.CreateElement("root");
            var tokenElement = root.InsertElement("token", token);
            var hashElement = root.InsertElement("hash", CalculateMD5Hash(parcel.parcel_code));
            var requestElement = root.InsertElement("request"); requestElement.AddAttribute("type", "pushOrdersStatusesChanged");
            var ordersIds = requestElement.InsertElement("ordersIds");

//            foreach (var parcel in parcels)
//            {
                var orderId = ordersIds.InsertElement("orderId");
                var yandexId = orderId.InsertElement("yandexId", parcel.parcel_code);
                var deliveryId = orderId.InsertElement("deliveryId", parcel.okod.ToString());
//            }
            xml.AppendChild(root);

           // _logger.Info("REQUEST:"+xml.Beautify());

            var task = RequestPostAsync(BaseAddress.AbsoluteUri, xml.OuterXml);

            var response = "";
            task.ContinueWith(t =>
                {
                    response = t.Result;
                    if (task.IsCompleted && response.Length > 0)
                    {
                        if (!response.Contains("<isError>false</isError>"))
                          _logger.Info("RESPONCE:\n" + response);
                        else
                          _logger.Info("Успешно!");

                    }
                    res = response;

                })
                .Wait(5 * 1000); // Ожидаем 5 секунд и выходим!
            return res;
        }
        public static string CalculateMD5Hash(string input)
        {
            var sb = new StringBuilder();
            // step 1, calculate MD5 hash from input
            using (var md5 = MD5.Create())
            {
                var inputBytes = Encoding.ASCII.GetBytes(input);
                var hash = md5.ComputeHash(inputBytes);

                // step 2, convert byte array to hex string

                foreach (var t in hash)
                    sb.Append(t.ToString("x2"));
            }
            return sb.ToString();
        }
    }
}
