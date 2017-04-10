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
using CsvHelper;
using System.Configuration;
using Newtonsoft.Json;

namespace CI_Satena
{
    public class Program
    {
        static void Main(string[] args)
        {

            List<AirportDef> _AirportsFrom = new List<AirportDef> { };
            List<AirportDef> _AirportsTo = new List<AirportDef> { };
            List<CIFLight> CIFLights = new List<CIFLight> { };

            string myDirData = AppDomain.CurrentDomain.BaseDirectory + "\\data";
            Directory.CreateDirectory(myDirData);

            string APIPathAirport = "airport/iata/";
            string APIPathAirline = "airline/iata/";
            Regex rgxIATAAirport = new Regex(@"([A-Z]{3})");
          
            const string ua = "Mozilla/5.0 (Windows NT 6.1; WOW64; Trident/7.0; rv:11.0) like Gecko";
            const string HeaderAccept = "text/html,application/xhtml+xml,application/xml;q=0.9,*;q=0.8";
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
                //using (HttpWebResponse responseIndex = (HttpWebResponse)request.GetResponse())
                using (StreamReader reader = new StreamReader(responseStream))
                {
                    Frontpage = reader.ReadToEnd();
                }
                //var reader = new StreamReader(responseStream);
                //var textResponse = reader.ReadToEnd();
                //Frontpage = textResponse;
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

            int FromDay = Convert.ToInt32(ConfigurationManager.AppSettings.Get("FromDay"));
            int ToDay = Convert.ToInt32(ConfigurationManager.AppSettings.Get("ToDay"));

            foreach (var From in _AirportsFrom)
            {
                foreach (var To in _AirportsTo)
                {
                    //Parallel.ForEach(_AirportsTo, new ParallelOptions { MaxDegreeOfParallelism = 2 }, (To) =>
                    //{
                    // Only ADZ to PVA AND back
                    if (From.IATA == "ÄDZ" & To.IATA != "PVA") { break; }
                    if (From.IATA == "PVA" & To.IATA != "ADZ") { break; }

                    if (From.IATA != To.IATA)
                    {
                        for (int i = FromDay; i < ToDay; i++)
                        {
                            // Response Variables
                            string ResponseForm = String.Empty;
                            string ResponseIndex = String.Empty;
                            string ResponseDias = String.Empty;
                            string ResponseRoutes = String.Empty;
                            CookieContainer cookieContainer = new CookieContainer();
                            CookieCollection cookieCollection = new CookieCollection();

                            

                            DateTime dateAndTime = DateTime.Now;
                            dateAndTime = dateAndTime.AddDays(i);
                            Console.WriteLine("{0} - {1} - {2}", From.Name, To.Name, dateAndTime.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture).ToString());

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
                            request.Proxy = null;

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
                            request.Proxy = null;

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

                            //request = (HttpWebRequest)WebRequest.Create("https://secure.kiusys.net/satena-ibe/resultados.php");

                            //var postDataDias = String.Format("tipoViaje=ida&accion=getDias");
                            //var dataDias = Encoding.ASCII.GetBytes(postDataDias);

                            //request.Method = "POST";
                            //request.ContentType = "application/x-www-form-urlencoded";
                            //request.ContentLength = dataDias.Length;
                            //request.UserAgent = ua;
                            //request.Referer = "https://secure.kiusys.net/satena-ibe/resultados.php";
                            //request.Headers.Add("Accept-Encoding", HeaderEncoding);
                            //request.Headers.Add("X-Requested-With", "XMLHttpRequest");
                            //request.Accept = "*/*";
                            //request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                            //request.CookieContainer = cookieContainer;

                            //using (var streamDias = request.GetRequestStream())
                            //{
                            //    streamDias.Write(dataDias, 0, dataDias.Length);
                            //}
                            //using (HttpWebResponse responseIndex = (HttpWebResponse)request.GetResponse())
                            //using (StreamReader reader = new StreamReader(responseIndex.GetResponseStream()))
                            //{
                            //    ResponseDias = reader.ReadToEnd();
                            //}
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
                            request.Accept = "*";
                            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                            request.CookieContainer = cookieContainer;
                            request.Proxy = null;

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
                                if (Convert.ToBoolean(ConfigurationManager.AppSettings.Get("Debug")))
                                {
                                    using (StreamWriter writetext = new StreamWriter(myDirData + "\\" + From.IATA + " - " + To.IATA  + " - " + dateAndTime.ToString("yyyyMMdd") + ".txt"))
                                    {
                                        writetext.Write(ResponseRoutes);
                                    }
                                } 
                                // Parsing Response Routes
                                HtmlDocument HtmlRoutes = new HtmlDocument();
                                HtmlRoutes.LoadHtml(ResponseRoutes);      
                                var RouteTable = HtmlRoutes.DocumentNode.SelectNodes("//table[@class='tabla2']//tr");
                                foreach (var Route in RouteTable)
                                {
                                    if (Route.SelectSingleNode("./td//div") != null)
                                    {
                                        // Check if route is direct or has a connection. only put direct routes in the gtfs feed.
                                        string routename = Route.SelectSingleNode("./td[1]/div[2]/span[1]").InnerText.ToString();
                                        if (routename.Contains(From.IATA) & routename.Contains(To.IATA))
                                        {
                                            // Direct Flight.
                                            string FlightDeparture = Route.SelectSingleNode("./td[1]/div[1]/span[1]").InnerText.ToString();
                                            string FlightArrival = Route.SelectSingleNode("./td[1]/div[1]/span[2]").InnerText.ToString();
                                            string TEMP_FlightNumber = Route.SelectSingleNode("./td[1]/div[3]/span[1]").InnerText.ToString();
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

                                }
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


            string gtfsDir = AppDomain.CurrentDomain.BaseDirectory + "\\gtfs";
            System.IO.Directory.CreateDirectory(gtfsDir);

            Console.WriteLine("Creating GTFS Files...");

            Console.WriteLine("Creating GTFS File agency.txt...");
            using (var gtfsagency = new StreamWriter(@"gtfs\\agency.txt"))
            {
                var csv = new CsvWriter(gtfsagency);
                csv.Configuration.Delimiter = ",";
                csv.Configuration.Encoding = Encoding.UTF8;
                csv.Configuration.TrimFields = true;
                // header 
                csv.WriteField("agency_id");
                csv.WriteField("agency_name");
                csv.WriteField("agency_url");
                csv.WriteField("agency_timezone");
                csv.WriteField("agency_lang");
                csv.WriteField("agency_phone");
                csv.WriteField("agency_fare_url");
                csv.WriteField("agency_email");
                csv.NextRecord();

                var airlines = CIFLights.Select(m => new { m.FlightAirline }).Distinct().ToList();

                for (int i = 0; i < airlines.Count; i++) // Loop through List with for)
                {
                    string urlapi = ConfigurationManager.AppSettings.Get("APIUrl") + APIPathAirline + airlines[0].FlightAirline.Trim();
                    string RequestAirlineJson = String.Empty;
                    HttpWebRequest requestAirline = (HttpWebRequest)WebRequest.Create(urlapi);

                    requestAirline.Method = "GET";
                    requestAirline.UserAgent = ua;
                    requestAirline.Accept = HeaderAccept;
                    requestAirline.Proxy = null;
                    requestAirline.KeepAlive = false;
                    using (HttpWebResponse Airlineresponse = (HttpWebResponse)requestAirline.GetResponse())
                    using (StreamReader reader = new StreamReader(Airlineresponse.GetResponseStream()))
                    {
                        RequestAirlineJson = reader.ReadToEnd();
                    }
                    dynamic AirlineResponseJson = JsonConvert.DeserializeObject(RequestAirlineJson);
                    csv.WriteField(Convert.ToString(AirlineResponseJson[0].code));
                    csv.WriteField(Convert.ToString(AirlineResponseJson[0].name));
                    csv.WriteField(Convert.ToString(AirlineResponseJson[0].website));
                    csv.WriteField("America/Bogota");
                    csv.WriteField("ES");
                    csv.WriteField(Convert.ToString(AirlineResponseJson[0].phone));
                    csv.WriteField("");
                    csv.WriteField("");
                    csv.NextRecord();
                }
            }

            Console.WriteLine("Creating GTFS File routes.txt ...");

            using (var gtfsroutes = new StreamWriter(@"gtfs\\routes.txt"))
            {
                // Route record


                var csvroutes = new CsvWriter(gtfsroutes);
                csvroutes.Configuration.Delimiter = ",";
                csvroutes.Configuration.Encoding = Encoding.UTF8;
                csvroutes.Configuration.TrimFields = true;
                // header 
                csvroutes.WriteField("route_id");
                csvroutes.WriteField("agency_id");
                csvroutes.WriteField("route_short_name");
                csvroutes.WriteField("route_long_name");
                csvroutes.WriteField("route_desc");
                csvroutes.WriteField("route_type");
                csvroutes.WriteField("route_url");
                csvroutes.WriteField("route_color");
                csvroutes.WriteField("route_text_color");
                csvroutes.NextRecord();


                var routes = CIFLights.Select(m => new { m.FromIATA, m.ToIATA, m.FlightAirline, m.FlightNumber }).Distinct().ToList();

                //for (int j = 0; j < routes.Count; j++)
                //{
                //    int FlightNumberOrg = Convert.ToInt16(routes[j].FlightNumber);
                //    if (IsEven(FlightNumberOrg))
                //    {
                //        // This is the flight from te base station
                //        // So the return flight in part of this route.
                //        // Return flight is flightnumber + 1
                //        int ReturnFlight = FlightNumberOrg + 1;
                //        routes.Remove(routes.Find(c => c.FromIATA == routes[j].ToIATA && c.ToIATA == routes[j].FromIATA && c.FlightAirline == routes[j].FlightAirline && c.FlightNumber == Convert.ToString(ReturnFlight)));                            
                //    }
                //    // Need to rework special cases like the ist - bog - pty - ist flight nr 800
                //}
                var routesdist = routes.Select(m => new { m.FromIATA, m.ToIATA, m.FlightAirline }).Distinct().ToList();
                //var routes = CIFLights.Select(m => new { m.FromIATA, m.ToIATA, m.FlightAirline }).Distinct().ToList();

                for (int i = 0; i < routesdist.Count; i++) // Loop through List with for)
                {
                    string FromAirportName = null;
                    string ToAirportName = null;
                    string FromAirportCountry = null;
                    string FromAirportContinent = null;
                    string ToAirportCountry = null;
                    string ToAirportContinent = null;

                    using (var clientFrom = new WebClient())
                    {
                        clientFrom.Encoding = Encoding.UTF8;
                        clientFrom.Headers.Add("user-agent", ua);
                        string urlapiFrom = ConfigurationManager.AppSettings.Get("APIUrl") + APIPathAirport + routesdist[i].FromIATA;
                        var jsonapiFrom = clientFrom.DownloadString(urlapiFrom);
                        dynamic AirportResponseJsonFrom = JsonConvert.DeserializeObject(jsonapiFrom);
                        FromAirportName = Convert.ToString(AirportResponseJsonFrom[0].name);
                        FromAirportCountry = Convert.ToString(AirportResponseJsonFrom[0].country_code);
                        FromAirportContinent = Convert.ToString(AirportResponseJsonFrom[0].continent);
                    }
                    using (var clientTo = new WebClient())
                    {
                        clientTo.Encoding = Encoding.UTF8;
                        clientTo.Headers.Add("user-agent", ua);
                        string urlapiTo = ConfigurationManager.AppSettings.Get("APIUrl") + APIPathAirport + routesdist[i].ToIATA;
                        var jsonapiTo = clientTo.DownloadString(urlapiTo);
                        dynamic AirportResponseJsonTo = JsonConvert.DeserializeObject(jsonapiTo);
                        ToAirportName = Convert.ToString(AirportResponseJsonTo[0].name);
                        ToAirportCountry = Convert.ToString(AirportResponseJsonTo[0].country_code);
                        ToAirportContinent = Convert.ToString(AirportResponseJsonTo[0].continent);
                    }

                    csvroutes.WriteField(routesdist[i].FromIATA + routesdist[i].ToIATA);
                    csvroutes.WriteField(routesdist[i].FlightAirline);
                    csvroutes.WriteField(routesdist[i].FromIATA + routesdist[i].ToIATA);
                    csvroutes.WriteField(FromAirportName + " - " + ToAirportName);
                    csvroutes.WriteField(""); // routes[i].FlightAircraft + ";" + CIFLights[i].FlightAirline + ";" + CIFLights[i].FlightOperator + ";" + CIFLights[i].FlightCodeShare
                    if (FromAirportCountry == ToAirportCountry)
                    {
                        // Colombian internal flight domestic
                        csvroutes.WriteField(1102);
                    }
                    else
                    {
                        if (FromAirportContinent == ToAirportContinent)
                        {
                            // International Flight
                            csvroutes.WriteField(1101);
                        }
                        else
                        {
                            // Intercontinental Flight
                            csvroutes.WriteField(1103);
                        }
                    }
                    csvroutes.WriteField("");
                    csvroutes.WriteField("");
                    csvroutes.WriteField("");
                    csvroutes.NextRecord();
                }
            }

            // stops.txt

            List<string> agencyairportsiata =
             CIFLights.SelectMany(m => new string[] { m.FromIATA, m.ToIATA })
                     .Distinct()
                     .ToList();

            using (var gtfsstops = new StreamWriter(@"gtfs\\stops.txt"))
            {
                // Route record
                var csvstops = new CsvWriter(gtfsstops);
                csvstops.Configuration.Delimiter = ",";
                csvstops.Configuration.Encoding = Encoding.UTF8;
                csvstops.Configuration.TrimFields = true;
                // header                                 
                csvstops.WriteField("stop_id");
                csvstops.WriteField("stop_name");
                csvstops.WriteField("stop_desc");
                csvstops.WriteField("stop_lat");
                csvstops.WriteField("stop_lon");
                csvstops.WriteField("zone_id");
                csvstops.WriteField("stop_url");
                csvstops.WriteField("stop_timezone");
                csvstops.NextRecord();

                for (int i = 0; i < agencyairportsiata.Count; i++) // Loop through List with for)
                {
                    // Using API for airport Data.
                    using (var client = new WebClient())
                    {
                        client.Encoding = Encoding.UTF8;
                        string urlapi = ConfigurationManager.AppSettings.Get("APIUrl") + APIPathAirport + agencyairportsiata[i];
                        var jsonapi = client.DownloadString(urlapi);
                        dynamic AirportResponseJson = JsonConvert.DeserializeObject(jsonapi);

                        csvstops.WriteField(Convert.ToString(AirportResponseJson[0].code));
                        csvstops.WriteField(Convert.ToString(AirportResponseJson[0].name));
                        csvstops.WriteField("");
                        csvstops.WriteField(Convert.ToString(AirportResponseJson[0].lat));
                        csvstops.WriteField(Convert.ToString(AirportResponseJson[0].lng));
                        csvstops.WriteField("");
                        csvstops.WriteField(Convert.ToString(AirportResponseJson[0].website));
                        csvstops.WriteField(Convert.ToString(AirportResponseJson[0].timezone));
                        csvstops.NextRecord();
                    }
                }
            }


            Console.WriteLine("Creating GTFS File trips.txt, stop_times.txt, calendar.txt ...");

            using (var gtfscalendar = new StreamWriter(@"gtfs\\calendar.txt"))
            {
                using (var gtfstrips = new StreamWriter(@"gtfs\\trips.txt"))
                {
                    using (var gtfsstoptimes = new StreamWriter(@"gtfs\\stop_times.txt"))
                    {
                        // Headers 
                        var csvstoptimes = new CsvWriter(gtfsstoptimes);
                        csvstoptimes.Configuration.Delimiter = ",";
                        csvstoptimes.Configuration.Encoding = Encoding.UTF8;
                        csvstoptimes.Configuration.TrimFields = true;
                        // header 
                        csvstoptimes.WriteField("trip_id");
                        csvstoptimes.WriteField("arrival_time");
                        csvstoptimes.WriteField("departure_time");
                        csvstoptimes.WriteField("stop_id");
                        csvstoptimes.WriteField("stop_sequence");
                        csvstoptimes.WriteField("stop_headsign");
                        csvstoptimes.WriteField("pickup_type");
                        csvstoptimes.WriteField("drop_off_type");
                        csvstoptimes.WriteField("shape_dist_traveled");
                        csvstoptimes.WriteField("timepoint");
                        csvstoptimes.NextRecord();

                        var csvtrips = new CsvWriter(gtfstrips);
                        csvtrips.Configuration.Delimiter = ",";
                        csvtrips.Configuration.Encoding = Encoding.UTF8;
                        csvtrips.Configuration.TrimFields = true;
                        // header 
                        csvtrips.WriteField("route_id");
                        csvtrips.WriteField("service_id");
                        csvtrips.WriteField("trip_id");
                        csvtrips.WriteField("trip_headsign");
                        csvtrips.WriteField("trip_short_name");
                        csvtrips.WriteField("direction_id");
                        csvtrips.WriteField("block_id");
                        csvtrips.WriteField("shape_id");
                        csvtrips.WriteField("wheelchair_accessible");
                        csvtrips.WriteField("bikes_allowed ");
                        csvtrips.NextRecord();

                        var csvcalendar = new CsvWriter(gtfscalendar);
                        csvcalendar.Configuration.Delimiter = ",";
                        csvcalendar.Configuration.Encoding = Encoding.UTF8;
                        csvcalendar.Configuration.TrimFields = true;
                        // header 
                        csvcalendar.WriteField("service_id");
                        csvcalendar.WriteField("monday");
                        csvcalendar.WriteField("tuesday");
                        csvcalendar.WriteField("wednesday");
                        csvcalendar.WriteField("thursday");
                        csvcalendar.WriteField("friday");
                        csvcalendar.WriteField("saturday");
                        csvcalendar.WriteField("sunday");
                        csvcalendar.WriteField("start_date");
                        csvcalendar.WriteField("end_date");
                        csvcalendar.NextRecord();

                        //1101 International Air Service
                        //1102 Domestic Air Service
                        //1103 Intercontinental Air Service
                        //1104 Domestic Scheduled Air Service


                        for (int i = 0; i < CIFLights.Count; i++) // Loop through List with for)
                        {

                            // Calender

                            csvcalendar.WriteField(CIFLights[i].FromIATA + CIFLights[i].ToIATA + CIFLights[i].FlightAirline + CIFLights[i].FlightNumber.Replace(" ", "") + String.Format("{0:yyyyMMdd}", CIFLights[i].FromDate) + String.Format("{0:yyyyMMdd}", CIFLights[i].ToDate) + Convert.ToInt32(CIFLights[i].FlightMonday) + Convert.ToInt32(CIFLights[i].FlightTuesday) + Convert.ToInt32(CIFLights[i].FlightWednesday) + Convert.ToInt32(CIFLights[i].FlightThursday) + Convert.ToInt32(CIFLights[i].FlightFriday) + Convert.ToInt32(CIFLights[i].FlightSaterday) + Convert.ToInt32(CIFLights[i].FlightSunday));
                            csvcalendar.WriteField(Convert.ToInt32(CIFLights[i].FlightMonday));
                            csvcalendar.WriteField(Convert.ToInt32(CIFLights[i].FlightTuesday));
                            csvcalendar.WriteField(Convert.ToInt32(CIFLights[i].FlightWednesday));
                            csvcalendar.WriteField(Convert.ToInt32(CIFLights[i].FlightThursday));
                            csvcalendar.WriteField(Convert.ToInt32(CIFLights[i].FlightFriday));
                            csvcalendar.WriteField(Convert.ToInt32(CIFLights[i].FlightSaterday));
                            csvcalendar.WriteField(Convert.ToInt32(CIFLights[i].FlightSunday));
                            csvcalendar.WriteField(String.Format("{0:yyyyMMdd}", CIFLights[i].FromDate));
                            csvcalendar.WriteField(String.Format("{0:yyyyMMdd}", CIFLights[i].ToDate));
                            csvcalendar.NextRecord();

                            // Trips
                            string FromAirportName = null;
                            string ToAirportName = null;
                            using (var client = new WebClient())
                            {
                                client.Encoding = Encoding.UTF8;
                                string urlapi = ConfigurationManager.AppSettings.Get("APIUrl") + APIPathAirport + CIFLights[i].FromIATA;
                                var jsonapi = client.DownloadString(urlapi);
                                dynamic AirportResponseJson = JsonConvert.DeserializeObject(jsonapi);
                                FromAirportName = Convert.ToString(AirportResponseJson[0].name);
                            }
                            using (var client = new WebClient())
                            {
                                client.Encoding = Encoding.UTF8;
                                string urlapi = ConfigurationManager.AppSettings.Get("APIUrl") + APIPathAirport + CIFLights[i].ToIATA;
                                var jsonapi = client.DownloadString(urlapi);
                                dynamic AirportResponseJson = JsonConvert.DeserializeObject(jsonapi);
                                ToAirportName = Convert.ToString(AirportResponseJson[0].name);
                            }
                            csvtrips.WriteField(CIFLights[i].FromIATA + CIFLights[i].ToIATA);
                            csvtrips.WriteField(CIFLights[i].FromIATA + CIFLights[i].ToIATA + CIFLights[i].FlightAirline + CIFLights[i].FlightNumber.Replace(" ", "") + String.Format("{0:yyyyMMdd}", CIFLights[i].FromDate) + String.Format("{0:yyyyMMdd}", CIFLights[i].ToDate) + Convert.ToInt32(CIFLights[i].FlightMonday) + Convert.ToInt32(CIFLights[i].FlightTuesday) + Convert.ToInt32(CIFLights[i].FlightWednesday) + Convert.ToInt32(CIFLights[i].FlightThursday) + Convert.ToInt32(CIFLights[i].FlightFriday) + Convert.ToInt32(CIFLights[i].FlightSaterday) + Convert.ToInt32(CIFLights[i].FlightSunday));
                            csvtrips.WriteField(CIFLights[i].FromIATA + CIFLights[i].ToIATA + CIFLights[i].FlightAirline + CIFLights[i].FlightNumber.Replace(" ", "") + String.Format("{0:yyyyMMdd}", CIFLights[i].FromDate) + String.Format("{0:yyyyMMdd}", CIFLights[i].ToDate) + Convert.ToInt32(CIFLights[i].FlightMonday) + Convert.ToInt32(CIFLights[i].FlightTuesday) + Convert.ToInt32(CIFLights[i].FlightWednesday) + Convert.ToInt32(CIFLights[i].FlightThursday) + Convert.ToInt32(CIFLights[i].FlightFriday) + Convert.ToInt32(CIFLights[i].FlightSaterday) + Convert.ToInt32(CIFLights[i].FlightSunday));
                            csvtrips.WriteField(ToAirportName);
                            csvtrips.WriteField(CIFLights[i].FlightAirline + CIFLights[i].FlightNumber);
                            csvtrips.WriteField("");
                            csvtrips.WriteField("");
                            csvtrips.WriteField("");
                            csvtrips.WriteField("1");
                            csvtrips.WriteField("");
                            csvtrips.NextRecord();

                            // Depart Record
                            csvstoptimes.WriteField(CIFLights[i].FromIATA + CIFLights[i].ToIATA + CIFLights[i].FlightAirline + CIFLights[i].FlightNumber.Replace(" ", "") + String.Format("{0:yyyyMMdd}", CIFLights[i].FromDate) + String.Format("{0:yyyyMMdd}", CIFLights[i].ToDate) + Convert.ToInt32(CIFLights[i].FlightMonday) + Convert.ToInt32(CIFLights[i].FlightTuesday) + Convert.ToInt32(CIFLights[i].FlightWednesday) + Convert.ToInt32(CIFLights[i].FlightThursday) + Convert.ToInt32(CIFLights[i].FlightFriday) + Convert.ToInt32(CIFLights[i].FlightSaterday) + Convert.ToInt32(CIFLights[i].FlightSunday));
                            csvstoptimes.WriteField(String.Format("{0:HH:mm:ss}", CIFLights[i].DepartTime));
                            csvstoptimes.WriteField(String.Format("{0:HH:mm:ss}", CIFLights[i].DepartTime));
                            csvstoptimes.WriteField(CIFLights[i].FromIATA);
                            csvstoptimes.WriteField("0");
                            csvstoptimes.WriteField("");
                            csvstoptimes.WriteField("0");
                            csvstoptimes.WriteField("0");
                            csvstoptimes.WriteField("");
                            csvstoptimes.WriteField("");
                            csvstoptimes.NextRecord();
                            

                            // Arrival Record
                            if (!CIFLights[i].FlightNextDayArrival)
                            {
                                csvstoptimes.WriteField(CIFLights[i].FromIATA + CIFLights[i].ToIATA + CIFLights[i].FlightAirline + CIFLights[i].FlightNumber.Replace(" ", "") + String.Format("{0:yyyyMMdd}", CIFLights[i].FromDate) + String.Format("{0:yyyyMMdd}", CIFLights[i].ToDate) + Convert.ToInt32(CIFLights[i].FlightMonday) + Convert.ToInt32(CIFLights[i].FlightTuesday) + Convert.ToInt32(CIFLights[i].FlightWednesday) + Convert.ToInt32(CIFLights[i].FlightThursday) + Convert.ToInt32(CIFLights[i].FlightFriday) + Convert.ToInt32(CIFLights[i].FlightSaterday) + Convert.ToInt32(CIFLights[i].FlightSunday));
                                csvstoptimes.WriteField(String.Format("{0:HH:mm:ss}", CIFLights[i].ArrivalTime));
                                csvstoptimes.WriteField(String.Format("{0:HH:mm:ss}", CIFLights[i].ArrivalTime));
                                csvstoptimes.WriteField(CIFLights[i].ToIATA);
                                csvstoptimes.WriteField("2");
                                csvstoptimes.WriteField("");
                                csvstoptimes.WriteField("0");
                                csvstoptimes.WriteField("0");
                                csvstoptimes.WriteField("");
                                csvstoptimes.WriteField("");
                                csvstoptimes.NextRecord();
                            }
                            else
                            {
                                //add 24 hour for the gtfs time
                                int hour = CIFLights[i].ArrivalTime.Hour;
                                hour = hour + 24;
                                int minute = CIFLights[i].ArrivalTime.Minute;
                                string strminute = minute.ToString();
                                if (strminute.Length == 1) { strminute = "0" + strminute; }
                                csvstoptimes.WriteField(CIFLights[i].FromIATA + CIFLights[i].ToIATA + CIFLights[i].FlightAirline + CIFLights[i].FlightNumber.Replace(" ", "") + String.Format("{0:yyyyMMdd}", CIFLights[i].FromDate) + String.Format("{0:yyyyMMdd}", CIFLights[i].ToDate) + Convert.ToInt32(CIFLights[i].FlightMonday) + Convert.ToInt32(CIFLights[i].FlightTuesday) + Convert.ToInt32(CIFLights[i].FlightWednesday) + Convert.ToInt32(CIFLights[i].FlightThursday) + Convert.ToInt32(CIFLights[i].FlightFriday) + Convert.ToInt32(CIFLights[i].FlightSaterday) + Convert.ToInt32(CIFLights[i].FlightSunday));
                                csvstoptimes.WriteField(hour + ":" + strminute + ":00");
                                csvstoptimes.WriteField(hour + ":" + strminute + ":00");
                                csvstoptimes.WriteField(CIFLights[i].ToIATA);
                                csvstoptimes.WriteField("2");
                                csvstoptimes.WriteField("");
                                csvstoptimes.WriteField("0");
                                csvstoptimes.WriteField("0");
                                csvstoptimes.WriteField("");
                                csvstoptimes.WriteField("");
                                csvstoptimes.NextRecord();
                            }
                        }
                    }
                }
            }

            // Create Zip File
            string startPath = gtfsDir;
            string zipPath = myDir + "\\Satena.zip";
            if (File.Exists(zipPath)) { File.Delete(zipPath); }
            ZipFile.CreateFromDirectory(startPath, zipPath, CompressionLevel.Fastest, false);

        }
        [Serializable]
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
