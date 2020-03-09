using System;
using System.IO;
using System.Net;
using HtmlAgilityPack;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Text;

namespace TeamFoodCrawler
{
    class Program
    {
        static string fileName = "TeamFood Speiseplan.txt";
        static string baseUrl = @"http://www.teamfood.eu/index.php?target=tf/speisekarte_dyn";

        static void Main(string[] args)
        {
            List<TeamFoodMenu> menu;
            string additionalInfo;

            // Hole Content-Node und extrahiere Inhalte
            var contentNode = GetContentNode();
            menu = GetMenuFromContent(contentNode);
            additionalInfo = GetAdditionalInfoFromContent(contentNode);

            // Schreibe in Datei
            saveMenuToFile(menu);

            // Schreibe in Konsole
            //printMenuToConsole(menu, additionalInfo);
        }

        /// <summary>
        /// Hole Inhalt der TeamFood Webseite mittels HtmlAgilityPack
        /// </summary>
        /// <returns>Relevanter Content-Knoten</returns>
        static HtmlNode GetContentNode()
        {
            // Hole Webinhalt
            HtmlDocument htmlDoc;
            string webPageContent;

            using (var client = new WebClient())
            {
                IWebProxy defaultWebProxy = WebRequest.DefaultWebProxy;
                defaultWebProxy.Credentials = CredentialCache.DefaultCredentials;
                client.Proxy = defaultWebProxy;
                webPageContent = client.DownloadString(baseUrl);
            }

            htmlDoc = new HtmlAgilityPack.HtmlDocument();
            htmlDoc.LoadHtml(webPageContent);

            // Hole relevante Informationen aus Webinhalt
            var contentNode = htmlDoc.GetElementbyId("col3_content");

            return contentNode;
        }

        /// <summary>
        /// Hole Zusatzinformationen aus HTML Knoten 
        /// </summary>
        /// <param name="contentNode"></param>
        /// <returns></returns>
        static string GetAdditionalInfoFromContent(HtmlNode contentNode)
        {
            var infos = contentNode.SelectNodes("p");

            if (infos.Count == 0) return "Kein Zusatzinfo gefunden";

            string additionalInfo = infos[0].InnerText;
            additionalInfo = additionalInfo.Replace("\t", " ");
            additionalInfo = additionalInfo.Replace("  ", " ");

            return additionalInfo;
        }

        /// <summary>
        /// Hole Menü aus HTML Knoten
        /// </summary>
        /// <param name="contentNode"></param>
        /// <returns></returns>
        static List<TeamFoodMenu> GetMenuFromContent(HtmlNode contentNode)
        {
            List<TeamFoodMenu> menu = new List<TeamFoodMenu>();
            Regex kwPattern = new Regex("(?<=KW )\\d{2}");
            Regex datePattern = new Regex("\\d{1,2}.\\d{1,2}.\\d{4}");

            NumberStyles style = NumberStyles.AllowDecimalPoint;
            CultureInfo provider = new CultureInfo("en-US");

            string[] foodTypes = Enum.GetNames(typeof(FoodType));


            // Speisepläne
            var tables = contentNode.SelectNodes("table");
            var headers = contentNode.SelectNodes("h3");

            if(headers.Count < tables.Count)
            {
                throw new Exception("Fehler beim Aufbau der Tabelle (zu wenige Überschriften).");
            }

            for(int i = 0; i < tables.Count; i++)
            {
                DateTime start;
                DateTime end;
                int kw;

                // Kalenderwoche
                string headerText = headers[i].InnerText;
                var kwMatch = kwPattern.Match(headerText);
                kw = int.Parse(kwMatch.Value);

                // Start- und Enddatum
                var dates = datePattern.Matches(headers[i].InnerText);
                if (dates.Count != 2)
                {
                    continue;
                    //throw new Exception("Fehler beim Parsen von Anfangs- und Enddatum");
                }
                start = DateTime.Parse(dates[0].Value);
                end = DateTime.Parse(dates[1].Value);

                TeamFoodMenu newMenu = new TeamFoodMenu(kw, start, end);

                // Gehe durch alle Tabellenzeilen
                var trs = tables[i].SelectNodes("tr");
                Wochentage currentDay = Wochentage.Montag;

                foreach (var tr in trs)
                {
                    var tds = tr.SelectNodes("td");

                    if (tds[0].InnerText == "Tag") continue;

                    TeamFoodMenuItem newMenuItem = new TeamFoodMenuItem();

                    // Extrahiere Wochentag
                    Wochentage parsedDay;
                    if (Enum.TryParse<Wochentage>(tds[0].InnerText, out parsedDay))
                    {
                        currentDay = parsedDay;
                    }

                    // Gericht
                    newMenuItem.Description = WebUtility.HtmlDecode(tds[1].InnerText);

                    // Typ / Fleischart
                    string type = WebUtility.HtmlDecode(tds[2].InnerText);
                    FoodType foodType = 0;
                    
                    // Gehe alle Namen der FoodTypes durch und füge die hinzu, die im Text vorkommen
                    for(int t = 0; t < foodTypes.Count(); t++)
                    {
                        if (type.Contains(foodTypes[t]))
                        {
                            foodType |= (FoodType) Enum.Parse(typeof(FoodType), foodTypes[t], true);
                            //foodType |= Enum.Parse<FoodType>(foodTypes[t], true);
                        }
                    }
                    // Falls keine FoodTypes im Text gefunden wurden, setze auf "Andere"
                    if (foodType == 0) foodType = FoodType.Andere;
                    newMenuItem.Type = foodType;

                    // Preis
                    string priceString = tds[3].InnerText.TrimEnd('€');
                    decimal price = 0;
                    decimal.TryParse(priceString, style, provider, out price);
                    newMenuItem.Price = price;

                    // Beilage
                    string side = WebUtility.HtmlDecode(tds[4].InnerText);
                    if (side != "-") { newMenuItem.SideDish = side; }
                    
                    // Zusatzstoffe
                    string additives = tds[5].InnerText;
                    if (additives.Contains(","))
                    {
                        int[] additivesArray = Array.ConvertAll(additives.Split(','), int.Parse);
                        newMenuItem.Additives = additivesArray.ToList();
                    }
                    else
                    {
                        newMenuItem.Additives = new List<int>();
                        int add;
                        if(int.TryParse(additives, out add)) newMenuItem.Additives.Add(add);
                    }


                    newMenu.AddMenuItem(currentDay, newMenuItem);
                }

                menu.Add(newMenu);
            }

            return menu;
        }

        /// <summary>
        /// Schreibt die extrahierten Informationen in die Konsole
        /// </summary>
        /// <param name="menuList"></param>
        /// <param name="additions"></param>
        static void printMenuToConsole(List<TeamFoodMenu> menu, string additions)
        {
            Console.OutputEncoding = Encoding.Default;

            // Schreibe Menüinhalte 
            foreach (var m in menu)
            {
                Console.WriteLine(m.ToString());
            }

            // Schreibe Liste der Zusätze
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("Zusätze:\n{0}\n{1}", new String('=', 8), additions);
            Console.WriteLine(sb.ToString());

            Console.ReadLine();
        }

        /// <summary>
        /// Schreibe die extrahierten Inhalte in eine Datei
        /// </summary>
        /// <param name="menu"></param>
        static void saveMenuToFile(List<TeamFoodMenu> menu)
        {
            // Bestimme Pfad und lösche evtl. vorhanden Datei
            string currentPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string path = Path.Combine(currentPath, fileName);
            File.Delete(path);

            File.WriteAllText(path, "teamfood\r\n");

            foreach (var m in menu)
            {
                foreach (var d in m.Days)
                {
                    File.AppendAllText(path, d.Wochentag.ToString() + "\n");

                    foreach(var mi in d.MenuItems)
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.AppendFormat("{0};;{1};{2};{3}\n", mi.Description, mi.SideDish, mi.Type, mi.Price);
                        File.AppendAllText(path, sb.ToString());
                    }                   
                }
            }
        }
    }
}
