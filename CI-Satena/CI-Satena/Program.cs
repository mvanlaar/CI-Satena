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
using System.Web;
using System.Globalization;

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
            List<CIFLight> CIFLights = new List<CIFLight> { };
            
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
                        _AirportsFrom.Add(new AirportDef { Name = AirportName, IATA = IATA, Value = AirportValue, FullName = Option });
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
                        _AirportsTo.Add(new AirportDef { Name = AirportName, IATA = IATA, Value = AirportValue, FullName = Option });
                    }
                }
            }
            //Fill in the form session

            foreach (var From in _AirportsFrom)
            {
                foreach (var To in _AirportsTo)
                {
                    //Parallel.ForEach(_AirportsTo, new ParallelOptions { MaxDegreeOfParallelism = 2 }, (To) =>
                    //{
                    if (From.IATA != To.IATA)
                    {
                        // Response Variables
                        string ResponseForm = String.Empty;
                        string ResponseIndex = String.Empty;
                        string ResponseDias = String.Empty;
                        string ResponseRoutes = String.Empty;
                        CookieContainer cookieContainer = new CookieContainer();
                        CookieCollection cookieCollection = new CookieCollection();

                        Console.WriteLine("{0} - {1}", From.Name, To.Name);

                        DateTime dateAndTime = DateTime.Now;
                        //dateAndTime = dateAndTime.AddDays(Day);


                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://secure.kiusys.net/satena-ibe/index.php");

                        var postDataIndex = String.Format("origen={0}&destino={1}&adultos=1&menores=0&infantes=0&fdesde={2}&fhasta=&trayecto=ida&accion=validar", From.Value, To.Value, dateAndTime.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture));
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
                        //Console.WriteLine("Fill in the form....");
                        // Fill in the form session
                        request = (HttpWebRequest)WebRequest.Create("https://secure.kiusys.net/satena-ibe/resultados.php");

                        var postData = String.Format("trayecto=ida&origen={0}&origenesIata={1}&destino={2}&destinosIata={3}&fdesde={4}&fhasta=&mayores=1&menores=0&infantes=0&consultar=1", HttpUtility.UrlEncode(From.FullName), HttpUtility.UrlEncode(From.Value), HttpUtility.UrlEncode(To.FullName), HttpUtility.UrlEncode(To.Value), HttpUtility.UrlEncode(dateAndTime.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)));
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

                        //Console.WriteLine("Post for days details...");
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
                        //Console.WriteLine("Post for route details...");
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
                        if (!ResponseRoutes.Contains("No hay vuelos"))
                        {
                            // Parsing Response Routes
                            HtmlDocument HtmlRoutes = new HtmlDocument();
                            HtmlRoutes.LoadHtml(ResponseRoutes);
                            var RouteTable = HtmlRoutes.DocumentNode.SelectSingleNode("//table[@class='tabla2']");
                            string FlightDeparture = RouteTable.SelectSingleNode("/table[1]/tr[2]/td[1]/div[1]/span[1]").InnerText.ToString();
                            string FlightArrival = RouteTable.SelectSingleNode("/table[1]/tr[2]/td[1]/div[1]/span[2]").InnerText.ToString();
                            string TEMP_FlightNumber = RouteTable.SelectSingleNode("/table[1]/tr[2]/td[1]/div[3]/span[1]").InnerText.ToString();
                            int start = TEMP_FlightNumber.IndexOf("(") + 1;
                            int end = TEMP_FlightNumber.IndexOf(")", start);
                            TEMP_FlightNumber = TEMP_FlightNumber.Substring(start, end - start);
                            Boolean TEMP_FlightMonday = false;
                            Boolean TEMP_FlightTuesday = false;
                            Boolean TEMP_FlightWednesday = false;
                            Boolean TEMP_FlightThursday = false;
                            Boolean TEMP_FlightFriday = false;
                            Boolean TEMP_FlightSaterday = false;
                            Boolean TEMP_FlightSunday = false;

                            int dayofweek = Convert.ToInt32(dateAndTime.DayOfWeek);
                            if (dayofweek == 0) { TEMP_FlightSunday = true; }
                            if (dayofweek == 1) { TEMP_FlightMonday = true; }
                            if (dayofweek == 2) { TEMP_FlightTuesday = true; }
                            if (dayofweek == 3) { TEMP_FlightWednesday = true; }
                            if (dayofweek == 4) { TEMP_FlightThursday = true; }
                            if (dayofweek == 5) { TEMP_FlightFriday = true; }
                            if (dayofweek == 6) { TEMP_FlightSaterday = true; }

                            // Add Flight to CIFlights
                            bool alreadyExists = CIFLights.Exists(x => x.FromIATA == From.IATA
                                && x.ToIATA == To.IATA
                                && x.FromDate == dateAndTime.Date
                                && x.ToDate == dateAndTime.Date
                                && x.FlightNumber == TEMP_FlightNumber
                                && x.ArrivalTime == DateTime.ParseExact(FlightArrival, "HH:mm", CultureInfo.InvariantCulture)
                                && x.DepartTime == DateTime.ParseExact(FlightDeparture, "HH:mm", CultureInfo.InvariantCulture)
                                && x.FlightAirline == "9R"
                                && x.FlightMonday == TEMP_FlightMonday
                                && x.FlightTuesday == TEMP_FlightTuesday
                                && x.FlightWednesday == TEMP_FlightWednesday
                                && x.FlightThursday == TEMP_FlightThursday
                                && x.FlightFriday == TEMP_FlightFriday
                                && x.FlightSaterday == TEMP_FlightSaterday
                                && x.FlightSunday == TEMP_FlightSunday);


                            if (!alreadyExists)
                            {
                                // don't add flights that already exists
                                CIFLights.Add(new CIFLight
                                {
                                    FromIATA = From.IATA,
                                    ToIATA = To.IATA,
                                    FromDate = dateAndTime.Date,
                                    ToDate = dateAndTime.Date,
                                    ArrivalTime = DateTime.ParseExact(FlightArrival, "HH:mm", CultureInfo.InvariantCulture),
                                    DepartTime = DateTime.ParseExact(FlightDeparture, "HH:mm", CultureInfo.InvariantCulture),
                                    //FlightAircraft = "A320",
                                    FlightAirline = "9R",
                                    FlightMonday = TEMP_FlightMonday,
                                    FlightTuesday = TEMP_FlightTuesday,
                                    FlightWednesday = TEMP_FlightWednesday,
                                    FlightThursday = TEMP_FlightThursday,
                                    FlightFriday = TEMP_FlightFriday,
                                    FlightSaterday = TEMP_FlightSaterday,
                                    FlightSunday = TEMP_FlightSunday,
                                    FlightNumber = TEMP_FlightNumber
                                    //FlightOperator = null,
                                    //FlightCodeShare = TEMP_FlightCodeShare,
                                    //FlightNextDayArrival = TEMP_FlightNextDayArrival,
                                    //FlightNextDays = TEMP_FlightNextDays
                                });
                            }
                        }
                    }
                    //});
                }
            }
            // Export XML
            // Write the list of objects to a file.
            System.Xml.Serialization.XmlSerializer writer =
            new System.Xml.Serialization.XmlSerializer(CIFLights.GetType());
            string myDir = AppDomain.CurrentDomain.BaseDirectory + "\\output";
            Directory.CreateDirectory(myDir);
            StreamWriter file =
               new System.IO.StreamWriter("output\\output.xml");

            writer.Serialize(file, CIFLights);
            file.Close();

        }

        public class CIFLight
        {
            // Auto-implemented properties. 
            public string FromIATA;
            public string ToIATA;
            public DateTime FromDate;
            public DateTime ToDate;
            public Boolean FlightMonday;
            public Boolean FlightTuesday;
            public Boolean FlightWednesday;
            public Boolean FlightThursday;
            public Boolean FlightFriday;
            public Boolean FlightSaterday;
            public Boolean FlightSunday;
            public DateTime DepartTime;
            public DateTime ArrivalTime;
            public String FlightNumber;
            public String FlightAirline;
            public String FlightOperator;
            public String FlightAircraft;
            public Boolean FlightCodeShare;
            public Boolean FlightNextDayArrival;
            public int FlightNextDays;
        }


        public class AirportDef
        {
            // Auto-implemented properties.  
            public string Name { get; set; }
            public string IATA { get; set; }
            public string Value { get; set; }
            public string FullName { get; set; }
        }
    }
}
