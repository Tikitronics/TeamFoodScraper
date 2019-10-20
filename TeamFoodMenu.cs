using System;
using System.Collections.Generic;
using System.Text;

namespace TeamFoodCrawler
{
    [Flags]
    public enum FoodType {Andere = 0, Schwein = 1, Rind = 2, Vegetarisch = 4, Geflügel = 8, Fisch = 16};
    public enum Wochentage { Montag, Dienstag, Mittwoch, Donnerstag, Freitag };

    // *****************************************
    // Ein Speiseplan für eine Woche
    // *****************************************
    class TeamFoodMenu
    {
        private int kw;
        private DateTime startDate;
        private DateTime endDate;
        private List<TeamFoodDay> days;

        public List<TeamFoodDay> Days
        {
            get { return days; }
            set { days = value; }
        }

        public int KW
        {
            get { return kw; }
            set { kw = value; }
        }

        public DateTime StartDate
        {
            get { return startDate; }
            set { startDate = value; }
        }

        public DateTime EndDate
        {
            get { return endDate; }
            set { endDate = value; }
        }

        public TeamFoodMenu(int kw, DateTime start, DateTime end)
        {
            // Guard Clause
            if(start == null | end == null | kw > 52)
            {
                throw new Exception("Falsche Übergabedaten an Kosturktor zu TeamFoodMenu");
            }

            KW = kw;
            StartDate = start;
            EndDate = end;
            days = new List<TeamFoodDay>();
        }

        public void AddMenuItem(Wochentage day, TeamFoodMenuItem menuItem)
        {
            if (menuItem == null) throw new Exception("Falsche Daten für MenuItem");
            if (!Enum.IsDefined(typeof(Wochentage), day)) throw new Exception("Ungültiger Wochentag angegeben.");

            TeamFoodDay teamFoodDay;

            teamFoodDay = days.Find(x => x.Wochentag == day);
            if (teamFoodDay == null)
            {
                teamFoodDay = new TeamFoodDay(day);
                days.Add(teamFoodDay);
            }

            teamFoodDay.AddMenuItem(menuItem);
        }

        // Gibt den Speiseplan als Text aus
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine();
            sb.AppendFormat("TeamFood Speiseplan für KW {0} ({1} - {2})\n", KW.ToString(), StartDate.ToShortDateString(), EndDate.ToShortDateString());
            sb.AppendLine(new String('=', 55));
            sb.AppendLine();
            foreach (var d in Days)
            {
                sb.Append(d.ToString());
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }

    // *****************************************
    // Ein Tag bei TeamFood
    // *****************************************
    public class TeamFoodDay
    {
        private Wochentage weekday;
        private List<TeamFoodMenuItem> menuItems;

        public Wochentage Wochentag
        {
            get { return weekday; }
            set { weekday = value; }
        }

        public List<TeamFoodMenuItem> MenuItems
        {
            get { return menuItems; }
            set { menuItems = value; }
        }

        public TeamFoodDay(Wochentage tag)
        {
            if (!Enum.IsDefined(typeof(Wochentage), tag)) throw new Exception("Falscher Wochentag angegeben.");
            Wochentag = tag;
            menuItems = new List<TeamFoodMenuItem>();
        }

        public void AddMenuItem(TeamFoodMenuItem menuItem)
        {
            if (menuItem == null ) throw new Exception("Falsche Daten für MenuItem");
            menuItems.Add(menuItem);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("{0}:\n", weekday);
            foreach(var mi in menuItems)
            {
                sb.Append(mi.ToString());
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }

    // *****************************************
    // Ein einzelner Eintrag in die Speiseliste
    // *****************************************
    public class TeamFoodMenuItem
    {
        private List<int> additives;
        private string description;
        private decimal price;
        private FoodType type;
        private string sideDish;

        public List<int> Additives
        {
            get { return additives; }
            set { additives = value; }
        }


        public FoodType Type
        {
            get { return type; }
            set { type = value; }
        }

        public decimal Price
        {
            get { return price; }
            set { price = value; }
        }

        public string Description
        {
            get { return description; }
            set 
            {
                description = value.Replace("  ", " "); 
            }
        }

        public string SideDish
        {
            get { return sideDish; }
            set { sideDish = value; }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("{0}\n", description);
            if (SideDish != String.Empty && SideDish != "-") sb.AppendFormat("{0}\n", SideDish);
            sb.AppendFormat("{0}€ / {1} / ", price, type);
            var additivesList = string.Join(", ", additives);
            sb.AppendFormat("Zusätze: {0}\n", additivesList);

            return sb.ToString();
        }
    }
}
