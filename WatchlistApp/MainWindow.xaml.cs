using DataModel;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.SQLite;
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

        protected virtual void OnPropertyChanged(string property_name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property_name));
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
            AddWatchlistDialog dialog = new AddWatchlistDialog(connection, this);
            dialog.ShowDialog();
        }

        private void AddShowToWatchlist(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var selected_watchlist = button?.DataContext as Watchlist;
            if (selected_watchlist != null)
            {
                AddShowDialog dialog = new AddShowDialog(connection, selected_watchlist, this);
                dialog.ShowDialog();
            }
        }

        private void DeleteWatchlist(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var watchlist_to_delete = button?.DataContext as Watchlist;

            if (watchlist_to_delete != null)
            {
                /* Alle Eintraege in der Verbindungstabelle */
                var watchlistshows = connection.GetTable<WatchlistShow>()
                    .Where(ws => ws.WlNr == watchlist_to_delete.WlNr).ToList();

                var shows_for_selected_watchlist = connection.GetTable<WatchlistShow>()
                        .Where(ws => ws.WlNr == watchlist_to_delete.WlNr)
                        .Join(connection.GetTable<Show>(),
                            ws => ws.ShowNr,
                            show => show.ShowNr,
                            (ws, show) => show)
                        .ToList();

                foreach (WatchlistShow watchlistshow in watchlistshows)
                {
                    connection.Delete(watchlistshow);
                }
                foreach (Show show in shows_for_selected_watchlist)
                {
                    connection.Delete(show);
                }
                connection.Delete(watchlist_to_delete);
                watchlists.Remove(watchlist_to_delete);
                shows?.Clear();
            }
            else
            {
                MessageBox.Show("[ERROR]: Fehler beim löschen der Watchlist!");
            }
        }

        private void RemoveShowFromWatchlist(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var show_to_delete = button?.DataContext as Show;
            if (show_to_delete != null) 
            {
                /* Watchlist, die die zu entfernende Show enthaelt */
                var watchlist_with_show_to_delete = connection.GetTable<WatchlistShow>()
                    .FirstOrDefault(ws => ws.ShowNr == show_to_delete.ShowNr);

                if (show_to_delete != null && watchlist_with_show_to_delete != null)
                {
                    connection.Delete(watchlist_with_show_to_delete);
                    connection.Delete(show_to_delete);
                    shows.Remove(show_to_delete);
                }
                return;
            }
            else
            {
                MessageBox.Show("[ERROR]: Fehler beim löschen der Show!");
            }
        }

        private void IsReleasingChecked(object sender, RoutedEventArgs e)
        {
            var check_box = sender as CheckBox;
            var show_to_update = check_box?.DataContext as Show;
            if (show_to_update != null)
            {
                show_to_update.IsReleasing = 1;
                connection.Update(show_to_update);
            }
            return;
        }

        private void IsReleasingUnchecked(object sender, RoutedEventArgs e)
        {
            var check_box = sender as CheckBox;
            var show_to_update = check_box?.DataContext as Show;
            if (show_to_update != null)
            {
                show_to_update.IsReleasing = 0;
                connection.Update(show_to_update);
            }
            return;
        }

        private void AlreadyWatchedChecked(object sender, RoutedEventArgs e)
        {
            var checl_box = sender as CheckBox;
            var show_to_update = checl_box?.DataContext as Show;
            if (show_to_update != null)
            {
                show_to_update.AlreadyWatched = 1;
                connection.Update(show_to_update);
            }
            return;
        }

        private void AlreadyWatchedUnchecked(object sender, RoutedEventArgs e)
        {
            var checl_box = sender as CheckBox;
            var show_to_update = checl_box?.DataContext as Show;
            if (show_to_update != null)
            {
                show_to_update.AlreadyWatched = 0;
                connection.Update(show_to_update);
            }
            return;
        }
    }
}