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
        private DataConnection connection = new DataConnection("SQLite", "Data Source=WatchlistDatabase.db");
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

        //Get Watchlist Table contents from Database and convert to ObservableCollection for visualisation with ListBox
        private void GetLists()
        {
            var watchlists_ = connection.GetTable<Watchlist>().ToList();
            watchlists = new ObservableCollection<Watchlist>(watchlists_); 
        }

        private void WatchlistSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Check if an item is selected
            if (watchlist_listbox.SelectedItem is Watchlist selectedWatchlist)
            {
                // Fetch the shows associated with the selected watchlist
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
    }
}