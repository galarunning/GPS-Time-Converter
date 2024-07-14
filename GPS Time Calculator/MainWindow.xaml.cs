using System;
using System.Windows;
using System.Globalization;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.DirectoryServices.ActiveDirectory;

namespace GPSConverterWPF
{
    public partial class MainWindow : Window
    {
        private readonly int MaxTOW = 604800;
        public string[] WeekDays { get; set; }
        public List<int> Hours { get; set; }
        public List<int> Minutes { get; set; }
        public List<int> Seconds { get; set; }  
        private static readonly DateTime Epoch = new DateTime(1980, 1, 6, 0, 0, 0, DateTimeKind.Utc);

        public List <int> LeapSecondsList { get; set; }
        public int[] DaysofMonths { get; set;}
        public string[] Months { get; set; }
        public List<int> Years { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            DaysofMonths = new int[] {
            1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22,
            23, 24, 25, 26, 27, 28, 29, 30, 31 };
            Months = new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };
            Years = new List<int>(Enumerable.Range(1980, DateTime.Now.Year - 1980 + 1));
            WeekDays = new string[] { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };
            Hours = new List<int>(Enumerable.Range(0, 24));
            Minutes = new List<int>(Enumerable.Range(0, 60));
            Seconds = new List<int>(Enumerable.Range(0, 60));
            LeapSecondsList = new List<int>(Enumerable.Range(1, 100));

            DataContext = this;
        }

        private (int weekDay, int hour, int minutes, int seconds, bool flag) CalculateTime(int tow)
        {
            if (tow >= 0 && tow <= 604800)
            {
                double towInHours = tow / 3600.0;
                int day = (int)(towInHours / 24);
                string weekDay = WeekDays[day % 7];
                int dayofWeek = Array.IndexOf(WeekDays, weekDay);

                towInHours -= day * 24;
                int hour = (int)towInHours;
                double minutesDec = (towInHours - hour) * 60;
                int minutes = (int)minutesDec;
                int seconds = (int)Math.Round((minutesDec - minutes) * 60);

                return (dayofWeek, hour, minutes, seconds, true);
            }
            else 
            {
                return (0, 0, 0, 0, false);
            }
        }

        private int CalculateTOW(string IndexWeekDay ,int hour, int minutes, int seconds)
        {
            int day = Array.IndexOf(WeekDays, IndexWeekDay);
            int tow = ((day * 24) * 3600) + (hour * 3600) + (minutes * 60) + seconds + int.Parse(LeapSecondSettings.Text);

            return tow;

        }

        private void ConvertTOW_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(TOWInput.Text, out int tow))
            {
                var (weekDay, hour, minutes, seconds, flag) = CalculateTime(tow);
                if (flag) { 
                    DateTime utcDateTime = new DateTime(2024, 1, 1)
                            .AddDays(weekDay - 1) 
                            .AddHours(hour)
                            .AddMinutes(minutes)
                            .AddSeconds(seconds);
                    utcDateTime = utcDateTime.AddSeconds(-int.Parse(LeapSecondSettings.Text));
                    AppConsole.AppendText($"TOW: {TOWInput.Text} <--> {utcDateTime.DayOfWeek}" +
                    $" {utcDateTime.Hour:D2}:{utcDateTime.Minute:D2}:{utcDateTime.Second:D2} UTC Time\n");
                }
                else if(flag == false)
                {
                    AppConsole.AppendText($"{TOWInput.Text} outside of boundary\n");
                    AppConsole.AppendText("Allowable TOW --> 0 < TOW < 604800\n");
                }
            }
            else
            {
                AppConsole.AppendText($"TOW not Valid!\nPlease enter number! You have entered {TOWInput.Text}\n");
            }
        }

        private void ConvertUTC_Click(object sender, RoutedEventArgs e)
        {
            // Convert UTC --> GPS Week
            int month = UTCMonthInput.SelectedIndex + 1;
            if (int.TryParse(UTCDayInput.Text, out int day) &&
                        int.TryParse(UTCYearInput.Text, out int year))
            {
                try
                {
                    var weekDiff = (new DateTime(year, month, day, 23, 59, 60 - int.Parse(LeapSecondSettings.Text), DateTimeKind.Utc) - Epoch).TotalDays / 7;
                    AppConsole.AppendText($"{day}-{Months[month - 1]}-{year} UTC Time <--> GPS Week Nu: {Math.Floor(weekDiff)}\n");
                }
                catch (Exception ex)
                {
                    AppConsole.AppendText($"Error: {ex.Message}\n{year} {month} {day}\n");
                }
            }
            else
            {
                AppConsole.AppendText("Invalid input! You need to enter a number!\n");
            }
        }

        private void ConvertGPSWeek_Click(object sender, RoutedEventArgs e)
        {
            // Convert GPSWeek --> UTC
            if (int.TryParse(GPSWeekNumberInput.Text, out int weekDiff))
            {
                var currentDate = Epoch.AddDays(weekDiff * 7);
                AppConsole.AppendText($"GPS Week Nu: {weekDiff} <--> Week Starting on: {currentDate.AddSeconds(-int.Parse(LeapSecondSettings.Text)):yyyy-MM-dd} UTC Time\n");
            }
            else
            {
                AppConsole.AppendText($"Invalid input! You need to enter a number!\nYou have entered: {GPSWeekNumberInput.Text}\n");
            }
        }

        private void ConvertWeek_Click(object sender, RoutedEventArgs e)
        {

            string WeekDay = WeekDayInput.Text;
            if(int.TryParse(WeekHourInput.Text, out int hour) &&
                int.TryParse(MinuteInput.Text, out int minute) &&
                int.TryParse(SecondInput.Text, out int seconds))
            {
                int tow = CalculateTOW(WeekDay, hour, minute, seconds);
                if (tow >= MaxTOW)
                {
                    tow = tow - MaxTOW;
                }
                AppConsole.AppendText($"{WeekDayInput.Text} {WeekHourInput.Text} {MinuteInput.Text} {SecondInput.Text} <--> TOW: {tow} in 1-seconds interval since start of GPS week.\n");
            }


        }
    }
}
