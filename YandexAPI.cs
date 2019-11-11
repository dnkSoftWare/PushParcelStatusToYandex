using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace YandexPUSH
{
    class YandexAPI: HttpClient
    {
        private XmlDocument xml;
        private string token = "1234";
        public YandexAPI(): base()
        {
            xml = new XmlDocument();
            
            XmlNode docNode = xml.CreateXmlDeclaration("1.0", "UTF-8", null);
            xml.AppendChild(docNode);

        }

        private async Task<string> RequestPostAsync(string uri, string xmlData)
        {
            string resp;
                var content = new ByteArrayContent(Encoding.UTF8.GetBytes(xmlData));
                content.Headers.Add("Content-Type", "text/plain");
                var response = await PostAsync(uri, content);
                response.EnsureSuccessStatusCode();
                resp = await response.Content.ReadAsStringAsync();
            return resp;
        }

        public string PushOrdersStatusesChanged(string url, Parcel  parcel)
        {
            var res = "";
            var root = xml.DocumentElement.InsertElement("root");
            var tokenElement = root.InsertElement("token", token);
            var hashElement = root.InsertElement("hash", parcel.parcel_id.GetHashCode().ToString());
            var requestElement = root.InsertElement("request"); requestElement.AddAttribute("type", "pushOrdersStatusesChanged");
            var ordersIds = requestElement.InsertElement("");
            var orderId = ordersIds.InsertElement("orderId");
            var yandexId = orderId.InsertElement("yandexId", parcel.parcel_code);
            var deliveryId = orderId.InsertElement("deliveryId", parcel.okod.ToString());

            var task = RequestPostAsync(url, xml.OuterXml);
            var response = "";
            task.ContinueWith(t =>
                {
                    response = t.Result;
                    if (task.IsCompleted && response.Length > 0)
                       // todo : логгер по результату запроса
                        res = response;
                })
                .Wait(30 * 1000); // Ожидаем 30 секунд и выходим!
            return res;
        }
    }
}
