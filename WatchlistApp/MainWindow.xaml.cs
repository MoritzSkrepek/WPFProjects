using DataModel;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.SQLite;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace WatchlistApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region Fields
        private readonly WatchlistDatabaseDb connection;
        public List<Tag> filters = new List<Tag>(); // Tags welche gefiltert werden sollen
        #endregion

        #region Properties
        public ObservableCollection<WatchlistShow> watchlist_shows { get; set; }
        public ObservableCollection<Tag> tags { get; set; } // Liste an Tags fuer das anzeigen in der UI

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

        public ObservableCollection<ShowViewModel> _show_view_models;
        public ObservableCollection<ShowViewModel> show_view_models
        {
            get { return _show_view_models; }
            set
            {
                _show_view_models = value;
                OnPropertyChanged(nameof(show_view_models));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        #endregion

        #region Constructor
        public MainWindow()
        {
            InitializeComponent();
            connection = new WatchlistDatabaseDb(new DataOptions<WatchlistDatabaseDb>(new DataOptions().UseSQLite("Data Source=../../../WatchlistDatabase.db")));
            DataContext = this;
            LoadData();
        }
        #endregion

        #region Data Loading
        private void LoadData()
        {
            watchlists = new ObservableCollection<Watchlist>(connection.GetTable<Watchlist>().ToList());
            tags = new ObservableCollection<Tag>(connection.GetTable<Tag>().ToList());
            show_view_models = new ObservableCollection<ShowViewModel>();
        }
        #endregion

        #region Event Handlers
        private void WatchlistSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            show_view_models.Clear();

            if (watchlist_listbox.SelectedItem is Watchlist selectedWatchlist)
            {
                var shows = GetShowsForWatchlist(selectedWatchlist);
                AddShowsToViewModel(shows);
            }
        }

        private void SearchShow(object sender, TextChangedEventArgs e)
        {
            show_view_models.Clear();

            if (watchlist_listbox.SelectedItem is Watchlist selectedWatchlist)
            {
                var shows = GetShowsForWatchlist(selectedWatchlist);

                if (!string.IsNullOrWhiteSpace(search_text_box.Text))
                {
                    shows = shows.Where(show => show.Name.Contains(search_text_box.Text)).ToList();
                }

                AddShowsToViewModel(shows);
            }
        }

        private void AddWatchlist(object sender, RoutedEventArgs e)
        {
            var dialog = new AddWatchlistDialog(connection, this);
            dialog.ShowDialog();
        }

        private void AddShowToWatchlist(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Watchlist selectedWatchlist)
            {
                var dialog = new AddShowDialog(connection, selectedWatchlist, tags, this);
                dialog.ShowDialog();
            }
        }

        private void DeleteWatchlist(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Watchlist watchlistToDelete)
            {
                DeleteWatchlistAndItsShows(watchlistToDelete);
            }
            else
            {
                MessageBox.Show("[ERROR]: Fehler beim Löschen der Watchlist!");
            }
        }

        private void RemoveShowFromWatchlist(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is ShowViewModel showViewModel)
            {
                RemoveShow(showViewModel);
            }
            else
            {
                MessageBox.Show("[ERROR]: Fehler beim Löschen der Show!");
            }
        }

        private void IsReleasingChecked(object sender, RoutedEventArgs e) => UpdateReleasingStatus(sender, e, true);
        private void IsReleasingUnchecked(object sender, RoutedEventArgs e) => UpdateReleasingStatus(sender, e, false);
        private void AlreadyWatchedChecked(object sender, RoutedEventArgs e) => UpdateWatchedStatus(sender, e, true);
        private void AlreadyWatchedUnchecked(object sender, RoutedEventArgs e) => UpdateWatchedStatus(sender, e, false);
        private void CurrentEpisodePlus(object sender, RoutedEventArgs e) => UpdateCurrentEpisodeStatus(sender, e, 1);
        private void CurrentEpisodeMinus(object sender, RoutedEventArgs e) => UpdateCurrentEpisodeStatus(sender, e, -1);

        private void SelectedFilterChanged(object sender, SelectionChangedEventArgs e)
        {
            filters.Clear();
            foreach (var item in filter_listbox.SelectedItems)
            {
                if (item is Tag tag)
                {
                    filters.Add(tag);
                }
                else
                {
                    return;
                }
            }
        }

        private void SearchClicked(object sender, RoutedEventArgs e)
        {
            if (filters.Count == 0)
            {
                WatchlistSelectionChanged(null, null);
                return;
            }

            if (watchlist_listbox.SelectedItem is not Watchlist selectedWatchlist)
            {
                MessageBox.Show("Bitte wählen Sie eine Watchlist aus.");
                return;
            }

            var matchedShows = GetFilteredShows(selectedWatchlist);
            show_view_models.Clear();
            AddShowsToViewModel(matchedShows);
        }
        #endregion

        #region Utility Methods
        private List<Show> GetShowsForWatchlist(Watchlist selectedWatchlist)
        {
            return connection.GetTable<WatchlistShow>()
                .Where(ws => ws.WlNr == selectedWatchlist.WlNr)
                .Join(connection.GetTable<Show>(),
                    ws => ws.ShowNr,
                    show => show.ShowNr,
                    (ws, show) => show)
                .ToList();
        }

        private void AddShowsToViewModel(List<Show> shows)
        {
            foreach (var show in shows)
            {
                var tagsForShow = GetTagsForShow(show.ShowNr);
                show_view_models.Add(new ShowViewModel
                {
                    show = show,
                    tags = new ObservableCollection<Tag>(tagsForShow)
                });
            }
        }

        private List<Tag> GetTagsForShow(long showNr)
        {
            return connection.GetTable<ShowTag>()
                .Where(st => st.ShowNr == showNr)
                .Join(connection.GetTable<Tag>(),
                    st => st.TagNr,
                    tag => tag.TagNr,
                    (st, tag) => tag)
                .ToList();
        }

        private void DeleteWatchlistAndItsShows(Watchlist watchlistToDelete)
        {
            var showsForWatchlist = GetShowsForWatchlist(watchlistToDelete);
            var watchlistShows = connection.GetTable<WatchlistShow>()
                .Where(ws => ws.WlNr == watchlistToDelete.WlNr)
                .ToList();

            foreach (var ws in watchlistShows)
            {
                connection.Delete(ws);
            }

            foreach (var show in showsForWatchlist)
            {
                var showTags = connection.GetTable<ShowTag>()
                    .Where(st => st.ShowNr == show.ShowNr)
                    .ToList();

                foreach (var showTag in showTags)
                {
                    connection.Delete(showTag);
                }
                connection.Delete(show);
            }

            connection.Delete(watchlistToDelete);
            watchlists.Remove(watchlistToDelete);
        }

        private void RemoveShow(ShowViewModel showViewModel)
        {
            var showToDelete = showViewModel.show;
            var watchlistShow = connection.GetTable<WatchlistShow>()
                .FirstOrDefault(ws => ws.ShowNr == showToDelete.ShowNr);

            var showTags = connection.GetTable<ShowTag>()
                .Where(st => st.ShowNr == showToDelete.ShowNr)
                .ToList();

            if (showToDelete != null && watchlistShow != null)
            {
                connection.Delete(watchlistShow);

                foreach (var tag in showTags)
                {
                    connection.Delete(tag);
                }

                connection.Delete(showToDelete);
                show_view_models.Remove(showViewModel);
            }
        }

        private void UpdateReleasingStatus(object sender, RoutedEventArgs e, bool isReleasing)
        {
            if (sender is CheckBox checkBox && checkBox.DataContext is ShowViewModel showViewModel)
            {
                showViewModel.show.IsReleasing = isReleasing ? 1 : 0;
                connection.Update(showViewModel.show);
            }
        }

        private void UpdateWatchedStatus(object sender, RoutedEventArgs e, bool alreadyWatched)
        {
            if (sender is CheckBox checkBox && checkBox.DataContext is ShowViewModel showViewModel)
            {
                showViewModel.show.AlreadyWatched = alreadyWatched ? 1 : 0;
                connection.Update(showViewModel.show);
            }
        }

        private void UpdateCurrentEpisodeStatus(object sender, RoutedEventArgs e, long addend)
        {
            if (sender is Button button && button.DataContext is ShowViewModel showViewModel)
            {
                showViewModel.show.CurrentEpisode += addend;
                connection.Update(showViewModel.show);
            }
        }

        private List<Show> GetFilteredShows(Watchlist selectedWatchlist)
        {
            var requiredTagCount = filters.Count;

            return connection.GetTable<WatchlistShow>()
                .Where(ws => ws.WlNr == selectedWatchlist.WlNr)
                .Join(connection.GetTable<Show>(),
                    ws => ws.ShowNr,
                    show => show.ShowNr,
                    (ws, show) => show)
                .Where(show =>
                    connection.GetTable<ShowTag>()
                        .Where(st => st.ShowNr == show.ShowNr && filters.Select(f => f.TagNr).Contains(st.TagNr))
                        .Count() == requiredTagCount)
                .ToList();
        }

        protected virtual void OnPropertyChanged(string property_name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property_name));
        #endregion
    }
}
