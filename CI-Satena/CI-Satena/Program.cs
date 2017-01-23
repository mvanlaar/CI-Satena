using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Text.RegularExpressions;
using System.IO.Compression;
using System.IO;
using System.Net;

namespace CI_Satena
{
    class Program
    {
        static void Main(string[] args)
        {
            // Get Frame for ticket origins 
            //https://secure.kiusys.net/satena-ibe/buscar.php
            List<AirportDef> _AirportsFrom = new List<AirportDef> { };
            List<AirportDef> _AirportsTo = new List<AirportDef> { };

            CookieContainer cookieContainer = new CookieContainer();
            CookieCollection cookieCollection = new CookieCollection();

            Regex rgxIATAAirport = new Regex(@"([A-Z]{3})");
          
            const string ua = "Mozilla/5.0 (Windows NT 6.1; WOW64; Trident/7.0; rv:11.0) like Gecko";
            const string HeaderAccept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            const string HeaderEncoding = "gzip,deflate";
            string Frontpage = String.Empty;
            //BrowserSession b = new BrowserSession();
            //Frontpage = b.Get("https://secure.kiusys.net/satena-ibe/buscar.php");
            //string Frontpage = String.Empty;
            Console.WriteLine("Get To and from desinations...");
            using (var webClient1 = new System.Net.WebClient())
            {
                webClient1.Headers.Add("user-agent", ua);
                webClient1.Headers.Add("Referer", "http://www.satena.com/");
                webClient1.Headers.Add("Accept-Encoding", HeaderEncoding);
                webClient1.Headers.Add("Accept", HeaderAccept);                
                //string destinationsurl = "http://easyfly.com.co/home/destinations?originID={0}";
                //Frontpage = destinationsurl.Replace("{0}", AirportFrom.Value);
                var responseStream = new GZipStream(webClient1.OpenRead("https://secure.kiusys.net/satena-ibe/buscar.php"), CompressionMode.Decompress);
                var reader = new StreamReader(responseStream);
                var textResponse = reader.ReadToEnd();
                Frontpage = textResponse;
            }
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(Frontpage);
            var nodesorg = doc.DocumentNode.SelectNodes("//select[@name='origen']/option");
            foreach (var node in nodesorg)
            {
                string Option = node.NextSibling.InnerText;
                if (Option.Contains("-"))
                { 
                    string[] OptionParts = Option.Split('-');
                    String AirportName = OptionParts[0];
                    string AirportValue = OptionParts[1];
                    string IATA = AirportValue.Trim();                
                    if (AirportValue != "0")
                    {
                        _AirportsFrom.Add(new AirportDef { Name = AirportName, IATA = IATA, Value = AirportValue });
                    }
                }
            }
            var nodesdest = doc.DocumentNode.SelectNodes("//select[@name='destino']/option");
            foreach (var node in nodesdest)
            {
                string Option = node.NextSibling.InnerText;
                if (Option.Contains("-"))
                {
                    string[] OptionParts = Option.Split('-');
                    String AirportName = OptionParts[0];
                    string AirportValue = OptionParts[1];
                    string IATA = AirportValue.Trim();
                    if (AirportValue != "0")
                    {
                        _AirportsTo.Add(new AirportDef { Name = AirportName, IATA = IATA, Value = AirportValue });
                    }
                }
            }
            //Fill in the form session

            // Response Variables
            string ResponseForm = String.Empty;
            string ResponseIndex = String.Empty;
            string ResponseDias = String.Empty;
            string ResponseRoutes = String.Empty;


            HttpWebRequest request = (HttpWebRequest) WebRequest.Create("https://secure.kiusys.net/satena-ibe/index.php");

            var postDataIndex = String.Format("origen= APO&destino= AUC&adultos=1&menores=0&infantes=0&fdesde=24/01/2017&fhasta=&trayecto=ida&accion=validar");
            var dataIndex = Encoding.ASCII.GetBytes(postDataIndex);

            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = dataIndex.Length;
            request.UserAgent = ua;
            request.Referer = "https://secure.kiusys.net/satena-ibe/buscar.php";
            request.Headers.Add("Accept-Encoding", HeaderEncoding);
            request.Accept = HeaderAccept;
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.CookieContainer = cookieContainer;

            using (var streamIndex = request.GetRequestStream())
            {
                streamIndex.Write(dataIndex, 0, dataIndex.Length);
            }
            using (HttpWebResponse responseIndex = (HttpWebResponse)request.GetResponse())
            using (StreamReader reader = new StreamReader(responseIndex.GetResponseStream()))
            {
                ResponseIndex = reader.ReadToEnd();
            }           
            //trayecto=ida&origen=BOGOTA+-+BOG&origenesIata=+BOG&destino=IPIALES+-+IPI&destinosIata=+IPI&fdesde=31%2F01%2F2017&fhasta=&mayores=1&menores=0&infantes=0&consultar=1
            Console.WriteLine("Fill in the form....");
            // Fill in the form session      
            

            request = (HttpWebRequest)WebRequest.Create("https://secure.kiusys.net/satena-ibe/resultados.php");

            var postData = String.Format("trayecto=ida&origen=BOGOTA+-+BOG&origenesIata=+BOG&destino=IPIALES+-+IPI&destinosIata=+IPI&fdesde=31%2F01%2F2017&fhasta=&mayores=1&menores=0&infantes=0&consultar=1");
            var data = Encoding.ASCII.GetBytes(postData);

            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = data.Length;
            request.UserAgent = ua;
            request.Referer = "https://secure.kiusys.net/satena-ibe/buscar.php";
            request.Headers.Add("Accept-Encoding", HeaderEncoding);
            request.Accept = HeaderAccept;
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.CookieContainer = cookieContainer;

            using (var streamPage = request.GetRequestStream())
            {
                streamPage.Write(data, 0, data.Length);
            }
            using (HttpWebResponse responseIndex = (HttpWebResponse)request.GetResponse())
            using (StreamReader reader = new StreamReader(responseIndex.GetResponseStream()))
            {
                ResponseForm = reader.ReadToEnd();
            }            
            // Posting dias

            Console.WriteLine("Post for days details...");
            request = (HttpWebRequest)WebRequest.Create("https://secure.kiusys.net/satena-ibe/resultados.php");

            var postDataDias = String.Format("tipoViaje=ida&accion=getDias");
            var dataDias = Encoding.ASCII.GetBytes(postDataDias);

            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = dataDias.Length;
            request.UserAgent = ua;
            request.Referer = "https://secure.kiusys.net/satena-ibe/resultados.php";
            request.Headers.Add("Accept-Encoding", HeaderEncoding);
            request.Headers.Add("X-Requested-With", "XMLHttpRequest");
            request.Accept = "*/*";
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.CookieContainer = cookieContainer;

            using (var streamDias = request.GetRequestStream())
            {
                streamDias.Write(dataDias, 0, dataDias.Length);
            }
            using (HttpWebResponse responseIndex = (HttpWebResponse)request.GetResponse())
            using (StreamReader reader = new StreamReader(responseIndex.GetResponseStream()))
            {
                ResponseDias = reader.ReadToEnd();
            }
            // Parse Routes
            Console.WriteLine("Post for route details...");
            request = (HttpWebRequest)WebRequest.Create("https://secure.kiusys.net/satena-ibe/resultados.php");

            var postDataRoutes = String.Format("tipoViaje=ida&accion=getRespuesta");
            var dataRoutes = Encoding.ASCII.GetBytes(postDataRoutes);

            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = dataRoutes.Length;
            request.UserAgent = ua;
            request.Referer = "https://secure.kiusys.net/satena-ibe/resultados.php";
            request.Headers.Add("Accept-Encoding", HeaderEncoding);
            request.Headers.Add("X-Requested-With", "XMLHttpRequest");
            request.Accept = "*/*";
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.CookieContainer = cookieContainer;

            using (var streamRoutes = request.GetRequestStream())
            {
                streamRoutes.Write(dataRoutes, 0, dataRoutes.Length);
            }
            using (HttpWebResponse responseIndex = (HttpWebResponse)request.GetResponse())
            using (StreamReader reader = new StreamReader(responseIndex.GetResponseStream()))
            {
                ResponseRoutes = reader.ReadToEnd();
            }
            // Parse Options
        }




        public class AirportDef
        {
            // Auto-implemented properties.  
            public string Name { get; set; }
            public string IATA { get; set; }
            public string Value { get; set; }
        }
    }
}
