using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace Quarta
{
    /// <summary>
    /// A basic page that provides characteristics common to most applications.
    /// </summary>
    public sealed partial class Play : Quarta.Common.LayoutAwarePage
    {
        public Play()
        {
            this.InitializeComponent();
        }

        #region State Management

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="navigationParameter">The parameter value passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested.
        /// </param>
        /// <param name="pageState">A dictionary of state preserved by this page during an earlier
        /// session.  This will be null the first time a page is visited.</param>
        protected override void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="pageState">An empty dictionary to be populated with serializable state.</param>
        protected override void SaveState(Dictionary<String, Object> pageState)
        {
        }

        #endregion

        #region Private variables and constants
        
        Thickness zeroThick = new Thickness(0);
        GridLength star = new GridLength(1, GridUnitType.Star);
        SolidColorBrush BlackBrush = new SolidColorBrush(Windows.UI.Colors.Black);
        SolidColorBrush WhiteBrush = new SolidColorBrush(Windows.UI.Colors.White);
        Brush[] Colors = new Brush[] { new SolidColorBrush(Color.FromArgb(255, 255, 127, 127)), new SolidColorBrush(Color.FromArgb(255, 127, 255, 127)), new SolidColorBrush(Color.FromArgb(255, 127, 127, 255)),
                                       new SolidColorBrush(Color.FromArgb(255, 255, 255, 127)), new SolidColorBrush(Color.FromArgb(255, 255, 127,255)), new SolidColorBrush(Color.FromArgb(255, 127, 255, 255)),     
                                       new SolidColorBrush(Color.FromArgb(255, 255, 53, 127)) };

        int FieldSize;
        bool isTimed;
        bool isSelected;
        int sx, sy;
        bool isOver;
        int[] HiScores;
        int Level;

        Random seed;
        GameLogic logic;        
        Button[,] Field;
        DispatcherTimer timer;
        

        int m_timeleft;
        int TimeLeft {
            get { return m_timeleft; }
            set
            {
                m_timeleft = value;
                if (m_timeleft < 0) m_timeleft = 0; 
                this.TimeLeftMonitor.Text = (m_timeleft / 60).ToString() + ":" + (m_timeleft % 60).ToString("00");
            }
        }


        #endregion

        #region Event handlers for interface

        private async void Play_Loaded(object sender, RoutedEventArgs e) {            
            SelectGameType();

            LoadHScores();
            UpgradeHighScore(0, true);
            
            InitLogic();
            
            var loaded = await Load();
            if (!loaded) Configure();

            InitGameField();

            if (loaded) logic.Continue();
            else logic.Start();

            if (isTimed) RestartTimer();

            isOver = false;
        }
        private void GameFieldButton_Click(object sender, RoutedEventArgs e) {
            Button b = sender as Button;
            var t = b.Tag as Tuple<int, int>;

            if (!isSelected) SelectBlock(t);
            else if (sx == t.Item1 || sy == t.Item2) SelectBlock(t);
            else if (!logic.Remove(sx, sy, t.Item1, t.Item2)) SelectBlock(t);
            else
            {
                UnselectBlock();
                if (isTimed) TimeLeft += 4;
            }
        }
        private void Hint_Click(object sender, RoutedEventArgs e) {
            int x1, x2, y1, y2;
            logic.Hint(out x1, out y1, out x2, out y2);

            for (int i = x1; i <= x2; i++) for (int j = y1; j <= y2; j++) Field[i, j].Opacity = 0.3;

            Task.Delay(2000).ContinueWith(x => this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, new Windows.UI.Core.DispatchedHandler(UnselectBlock)));

        }
        private async void Restart_Click(object sender, RoutedEventArgs e) {
            ApplicationData.Current.RoamingSettings.Values.Remove(isTimed ? "tsd" : "gsd");
            if (await Question.Show("Are you sure you want to restart?", "Quarta"))
            {
                logic.Restart();
                isOver = false;
                if (isTimed) RestartTimer();
            }
        }
        private void Shuffle_Click(object sender, RoutedEventArgs e) {
            logic.Shuffle(isTimed);
            if (isTimed) { TimeLeft -= 5; }
        }
        protected override void GoBack(object sender, RoutedEventArgs e)        
        {
            if (isTimed) timer.Stop();
 	        base.GoBack(sender, e);
        }
        #endregion

        #region Game Logic related Event Handlers

        private void timer_Tick(object sender, object e) {
            TimeLeft -= 1;
            if (TimeLeft <= 0) GameOver();
        }
        private void Logic_Changed(object sender, IntPoint e) {
            Field[e.X, e.Y].Background = BlackBrush;
            Field[e.X, e.Y].Opacity = 0;
            
            Task.Delay(200).ContinueWith(
                x => this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, 
                    new Windows.UI.Core.DispatchedHandler(
                        () => { Field[e.X, e.Y].Background = Colors[e.Data]; Field[e.X, e.Y].Opacity = 1; })));
            
            Save();
        }
        private void Logic_ScoreChanged(object sender, int e) {
            this.pointsTitle.Text = e.ToString();
            UpgradeHighScore(e);
        }
        private void Logic_NoMoves(object sender, EventArgs e) {
            if (isTimed) logic.Shuffle(true);
            else GameOver();
        }

        #endregion

        #region Interface-related functions
       
        private void InitGameField()        
        {
            GameField.ColumnDefinitions.Clear();
            GameField.RowDefinitions.Clear();
            for (int i = 0; i < FieldSize; i++)
            {
                GameField.ColumnDefinitions.Add(new ColumnDefinition { Width = star });
                GameField.RowDefinitions.Add(new RowDefinition { Height = star });
            }
            Field = new Button[FieldSize, FieldSize];
            for (int i = 0; i < FieldSize; i++) for (int j = 0; j < FieldSize; j++) CreateButton(i, j);
        }
        private void CreateButton(int i, int j)
        {
            var b = new Button
            {
                HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Stretch,
                VerticalAlignment = Windows.UI.Xaml.VerticalAlignment.Stretch,
                Tag = new Tuple<int, int>(i, j),
                Style = Application.Current.Resources["Unbordered"] as Style
            };
            b.Click += GameFieldButton_Click;
            var g = new Border { Background = WhiteBrush, Child = b, Margin = new Thickness(3), Padding = zeroThick };
            g.SetValue(Grid.RowProperty, i);
            g.SetValue(Grid.ColumnProperty, j);
            GameField.Children.Add(g);
            Field[i, j] = b;
        }
        private void SelectBlock(Tuple<int, int> t) { SelectBlock(t.Item1, t.Item2); }
        private void SelectBlock(int x, int y)
        {
            UnselectBlock();
            Field[x, y].Opacity = 0.2;
            sx = x; sy = y;
            isSelected = true;
        }
        private void UnselectBlock()
        {
            isSelected = false;
            for (int i = 0; i < FieldSize; i++)
                for (int j = 0; j < FieldSize; j++)
                    Field[i, j].Opacity = 1;
        }
        void UpgradeHighScore(int Value, bool force = false)
        {
            if (force) hiscoreTitle.Text = HiScores[isTimed ? Level : 7 + Level].ToString();
            if (HiScores[isTimed ? Level : 7 + Level] > Value) return;

            HiScores[isTimed ? Level : 7 + Level] = Value;
            hiscoreTitle.Text = Value.ToString();
            
            SaveHScores();
        }      
        private async void GameOver()
        {
            if (isTimed) timer.Stop();
            var key = isTimed ? "tsd" : "gsd";
            ApplicationData.Current.RoamingSettings.Values.Remove(key);            
            isOver = true;
            if (await Question.Show("Game over. Do you want to play again?", "Quarta"))
                logic.Restart();
        }

        #endregion

        #region Logic-related functions

        void InitLogic()
        {
            logic = new GameLogic();

            logic.Changed += Logic_Changed;
            logic.NoMoves += Logic_NoMoves;
            logic.ScoreChanged += Logic_ScoreChanged;
        }
        private void Configure()
        {
            var rs = ApplicationData.Current.RoamingSettings.Values;
            int mc = 0;

            switch (Level)
            {
                case 0: FieldSize = 13; mc = 3; break;
                case 1: FieldSize = 13; mc = 4; break;
                case 2: FieldSize = 17; mc = 4; break;
                case 3: FieldSize = 17; mc = 5; break;
                case 4: FieldSize = 17; mc = 6; break;
                case 5: FieldSize = 19; mc = 6; break;
                case 6: FieldSize = 19; mc = 7; break;
                default: FieldSize = 17; mc = 4; break;
            }
            logic.Init(FieldSize, mc);
        }
        private async Task<bool> Load()
        {
            var rs = ApplicationData.Current.RoamingSettings.Values;
            var key = isTimed ? "tsd" : "gsd";
            if (!rs.ContainsKey(key)) return false;

            if (!rs.ContainsKey(key)) return false;
            if (!await Question.Show("You have game saved from previous time. Do you want to continue or start from scratch?", "Quarta", "Continue", "Restart")) return false;

            logic.Load((string)rs[key]);

            FieldSize = logic.FieldSize;

            if (rs.ContainsKey("timeleft")) TimeLeft = (int)rs["timeleft"];
            return true;
        }
        private void Save()
        {
            var rs = ApplicationData.Current.RoamingSettings.Values;
            var key = isTimed ? "tsd" : "gsd";

            rs[key] = logic.Save();
            if (isTimed) rs["timeleft"] = TimeLeft; else rs.Remove("timeleft");
        }
        private void SaveHScores()
        {
            string res = HiScores[0].ToString();
            for (int i = 1; i < HiScores.Length; i++) res += ":" + HiScores[i].ToString();
            ApplicationData.Current.RoamingSettings.Values["hiscores"] = res;
        }
        private void LoadHScores()
        {
            var rs = ApplicationData.Current.RoamingSettings.Values;
            if (!rs.ContainsKey("hiscores")) {
                HiScores = new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            } else {
                var res = ((string)rs["hiscores"]).Split(':');
                HiScores = new int[res.Length];
                for (int i = 0; i < res.Length; i++) HiScores[i] = int.Parse(res[i]);
            }
        }

        private void RestartTimer()
        {
            if (TimeLeft == 0) TimeLeft = 60;
            timer.Start();            
        }

        private void SelectGameType()
        {            
            var rs = ApplicationData.Current.RoamingSettings.Values;
            Level = rs.ContainsKey("level") ? (int)rs["level"] : 0;
            if ((string)rs["type"] == "t")
            {
                isTimed = true;
                timer = new DispatcherTimer();
                timer.Interval = new TimeSpan(0, 0, 1);
                timer.Tick += timer_Tick;     
                TimerFrame.Visibility = Visibility.Visible;
            }
            else
            {
                TimerFrame.Visibility = Visibility.Collapsed;
                isTimed = false;
            }
        }
        
        #endregion

        bool torestore = false;

        private async void pageRoot_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (isTimed && e.NewSize.Width < 500)
            {
                torestore = true;
                timer.Stop();
            }
            else if (isTimed && torestore && e.NewSize.Width > 700)
            {
                torestore = false;
                timer.Start();
            }
        }
    }
}
