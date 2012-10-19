using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace Quarta
{
    /// <summary>
    /// A basic page that provides characteristics common to most applications.
    /// </summary>
    public sealed partial class ChangeDifficulty : Quarta.Common.LayoutAwarePage
    {
        public ChangeDifficulty()
        {
            this.InitializeComponent();

           var roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings.Values;
            if (!roamingSettings.ContainsKey("level"))
                roamingSettings["level"] = 0;
        }



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

        bool Do = false;


        private void ChangeDifficulty_Loaded(object sender, RoutedEventArgs e)
        {
            LevelsView.ItemsSource = Levels.Items;

            var roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings.Values;

            if (roamingSettings.ContainsKey("level")) LevelsView.SelectedIndex = (int)roamingSettings["level"];
            else LevelsView.SelectedIndex = 0;

            Do = true;
        }


        private void Levels_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SaveGoBack();
        }

        async void SaveGoBack()
        {
            if (!Do) return;

            var roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings.Values;


            roamingSettings["level"] = LevelsView.SelectedIndex;

            Task.Delay(250).ContinueWith(new Action<Task>((x) => this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () => Frame.GoBack())));

            roamingSettings["first"] = true;

            Do = false;
        }

        private void LevelsView_Tapped(object sender, TappedRoutedEventArgs e)
        {
            SaveGoBack();
        }
    }

    public class Level
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class Levels
    {
        static IEnumerable<Level> levels;
        private static readonly object locker = new object();
        public static IEnumerable<Level> Items
        {
            get
            {
                if (levels != null) return levels;
                Monitor.Enter(locker);

                var t = new List<Level>();

                t.Add(new Level { Name = "Novice", Description = "13x13, 3 colors" });
                t.Add(new Level { Name = "Beginner", Description = "13x13, 4 colors" });
                t.Add(new Level { Name = "Intermediate", Description = "17x17, 4 colors" });
                t.Add(new Level { Name = "Advanced", Description = "17x17, 5 colors" });
                t.Add(new Level { Name = "Expert", Description = "17x17, 6 colors" });
                t.Add(new Level { Name = "Insane", Description = "19x19, 6 colors" });
                t.Add(new Level { Name = "Jedy", Description = "19x19, 7 colors" });

                Interlocked.Exchange(ref levels, t);
                Monitor.Exit(locker);

                return levels;
            }
        }

    }
}
