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
        static string path;

        static void Main(string[] args)
        {
            string currentPath;

            Console.OutputEncoding = Encoding.Default;
            
            currentPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            path = Path.Combine(currentPath, fileName);
            File.Delete(path);

            GetAgileMenu();

            Console.ReadLine();
        }

        static void GetAgileMenu()
        {
            List<TeamFoodMenu> menu;
            string additionalInfo;

            // Hole Webinhalt
            HtmlWeb web = new HtmlWeb();
            var htmlDoc = web.Load(baseUrl);

            // Hole relevante Informationen aus Webinhalt
            var contentNode = htmlDoc.GetElementbyId("col3_content");

            additionalInfo = GetAdditionalInfoFromContent(contentNode);
            menu = GetMenuFromContent(contentNode);

            foreach( var m in menu)
            {
                Console.WriteLine(m.ToString());
                File.AppendAllText(path, m.ToString());
            }

            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("Zusätze:\n{0}\n{1}", new String('=', 8), additionalInfo);

            Console.WriteLine(sb.ToString());
            File.AppendAllText(path, sb.ToString());
        }

        // *****************************************
        // Hole Zusatzinformationen aus HTML Knoten
        // *****************************************
        static string GetAdditionalInfoFromContent(HtmlNode contentNode)
        {
            var infos = contentNode.SelectNodes("p");

            if (infos.Count == 0) return "Kein Zusatzinfo gefunden";

            string additionalInfo = infos[0].InnerText;
            additionalInfo = additionalInfo.Replace("\t", " ");
            additionalInfo = additionalInfo.Replace("  ", " ");

            return additionalInfo;
        }

        // *****************************************
        // Hole Menü aus HTML Knoten
        // *****************************************
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
                if (dates.Count != 2) throw new Exception("Fehler beim Parsen von Anfangs- und Enddatum");
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
                    decimal price = decimal.Parse(priceString, style, provider);
                    newMenuItem.Price = price;

                    // Beilage
                    string side = WebUtility.HtmlDecode(tds[4].InnerText);
                    newMenuItem.SideDish = side;

                    // Zusatzstoffe
                    string additives = tds[5].InnerText;
                    int[] additivesArray = Array.ConvertAll(additives.Split(','), int.Parse);
                    newMenuItem.Additives = additivesArray.ToList();

                    newMenu.AddMenuItem(currentDay, newMenuItem);
                }

                menu.Add(newMenu);
            }

            return menu;
        }

    }


    
}
