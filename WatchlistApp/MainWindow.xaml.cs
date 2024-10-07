using DataModel;
using LinqToDB;
using LinqToDB.Data;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WatchlistApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private readonly WatchlistDatabaseDb connection = new(
            new DataOptions<WatchlistDatabaseDb>(
                new DataOptions().UseSQLite("Data Source=../../../WatchlistDatabase.db")
            )
        );

        public event PropertyChangedEventHandler? PropertyChanged;
        public ObservableCollection<WatchlistShow> watchlist_shows { get; set; }

        public ObservableCollection<Watchlist> _watchlists;
        public ObservableCollection<Watchlist> watchlists 
        {
            get { return _watchlists; }
            set 
            { 
                _watchlists = value;
                OnPropertyChanged(nameof(watchlists));
            }
        }

        public ObservableCollection<Show> _shows;
        public ObservableCollection<Show> shows 
        {
            get { return _shows; }
            set
            {
                _shows = value;
                OnPropertyChanged(nameof(shows));
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public MainWindow()
        {
            InitializeComponent();
            GetLists();
            DataContext = this;
        }

        private void GetLists()
        {
            var watchlists_ = connection.GetTable<Watchlist>()
                .ToList();
            watchlists = new ObservableCollection<Watchlist>(watchlists_); 
        }

        private void WatchlistSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (watchlist_listbox.SelectedItem is Watchlist selectedWatchlist)
            {
                /* Alle Shows die in der aktuell ausgewaehlten Watchlist enthalten
                 * sind abfragen und anzeigen */
                var showsForSelectedWatchlist = connection.GetTable<WatchlistShow>()
                    .Where(ws => ws.WlNr == selectedWatchlist.WlNr)
                    .Join(connection.GetTable<Show>(),
                        ws => ws.ShowNr,
                        show => show.ShowNr,
                        (ws, show) => show)
                    .ToList();
                shows = new ObservableCollection<Show>(showsForSelectedWatchlist);
            }
        }

        private void SearchShow(object sender, RoutedEventArgs e)
        {
            if (watchlist_listbox.SelectedItem is Watchlist selectedWatchlist)
            {
                var query = connection.GetTable<WatchlistShow>()
                    .Where(ws => ws.WlNr == selectedWatchlist.WlNr)
                    .Join(connection.GetTable<Show>(),
                        ws => ws.ShowNr,
                        show => show.ShowNr,
                        (ws, show) => show);

                /* Wenn ein Suchbegriff eingegeben ist, dann aus der Menge an 
                 * Shows aus der Watchlist die Shows mit dem Kriterium anzeigen */
                if (!string.IsNullOrWhiteSpace(search_text_box.Text))
                {
                    query = query.Where(s => s.Name.Contains(search_text_box.Text));
                }

                /* Die gefilterten Shows zu einer Liste verarbeiten und anschliessend
                 * anzeigen */
                var searched_shows = query.ToList();
                shows = new ObservableCollection<Show>(searched_shows);
            }
        }

        private void AddWatchlist(object sender, RoutedEventArgs e)
        {
            AddWatchlistDialog dialog = new AddWatchlistDialog(connection);
            if(dialog.ShowDialog() == true)
            {
                watchlists.Add(dialog.new_watch_list);
            }
        }

        private void DeleteWatchlist(object sender, RoutedEventArgs e)
        {
            /* TODO: Delete whole watchlist */
        }

        private void AddShowToWatchlist(object sender, RoutedEventArgs e)
        {
            /* Button welcher geklickt wurde */ 
            var button = sender as Button;

            /* Watchlist von dem DataContext des Buttons holen */
            var selectedWatchlist = button?.DataContext as Watchlist;

            /* TODO: Add show to watchlist dialog and logic */
        }

        private void RemoveShowFromWatchlist(object sender, RoutedEventArgs e)
        {
            /* Button welcher geklickt wurde */
            var button = sender as Button;

            /* Show von dem DataContext des Buttons holen */
            var show_to_delete = button?.DataContext as Show;

            /* Watchlist, die die zu entfernende Show enthaelt */
            var watchlist_with_show_to_delete = connection.GetTable<WatchlistShow>()
                .FirstOrDefault(ws => ws.ShowNr == show_to_delete.ShowNr);

            connection.Delete(watchlist_with_show_to_delete);
            connection.Delete(show_to_delete);
            shows.Remove(show_to_delete);     
        }
    }
}