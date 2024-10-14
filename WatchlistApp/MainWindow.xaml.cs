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

        public List<Tag> filters = new List<Tag>(); // Tags welche gefiltert werden sollen
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

        public ObservableCollection<ShowViewModel> _show_view_model;
        public ObservableCollection<ShowViewModel> show_view_model 
        {
            get { return _show_view_model; }
            set
            {
                _show_view_model = value;
                OnPropertyChanged(nameof(show_view_model));
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
            var _watchlists = connection.GetTable<Watchlist>().ToList();
            watchlists = new ObservableCollection<Watchlist>(_watchlists); 
            var _tags = connection.GetTable<Tag>().ToList();
            tags = new ObservableCollection<Tag>(_tags);
            show_view_model = new ObservableCollection<ShowViewModel>();
        }

        private void WatchlistSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            show_view_model.Clear();
            if (watchlist_listbox.SelectedItem is Watchlist selectedWatchlist)
            {
                // Alle Shows, die in der aktuell ausgewählten Watchlist enthalten sind
                var showsForSelectedWatchlist = connection.GetTable<WatchlistShow>()
                    .Where(ws => ws.WlNr == selectedWatchlist.WlNr)
                    .Join(connection.GetTable<Show>(),
                        ws => ws.ShowNr,
                        show => show.ShowNr,
                        (ws, show) => show)
                    .ToList();
                
                foreach (var show in showsForSelectedWatchlist)
                {
                    var tagsForShow = connection.GetTable<ShowTag>()
                        .Where(st => st.ShowNr == show.ShowNr)
                        .Join(connection.GetTable<Tag>(),
                            st => st.TagNr,
                            tag => tag.TagNr,
                            (st, tag) => tag)
                        .ToList();

                    show_view_model.Add(new ShowViewModel
                    {
                        show = new Show()
                        {
                            ShowNr = show.ShowNr,
                            Name = show.Name,
                            Description = show.Description,
                            ReleaseDate = show.ReleaseDate,
                            IsReleasing = show.IsReleasing,
                            AlreadyWatched = show.AlreadyWatched,
                            Image = show.Image
                        },
                        tags = new ObservableCollection<Tag>(tagsForShow)
                    });
                }
            }
        }

        private void SearchShow(object sender, TextChangedEventArgs e)
        {
            show_view_model.Clear();
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

                var searched_shows = query.ToList();
                foreach (var show in searched_shows)
                {
                    var tagsForShow = connection.GetTable<ShowTag>()
                        .Where(st => st.ShowNr == show.ShowNr)
                        .Join(connection.GetTable<Tag>(),
                            st => st.TagNr,
                            tag => tag.TagNr,
                            (st, tag) => tag)
                        .ToList();

                    show_view_model.Add(new ShowViewModel
                    {
                        show = new Show()
                        {
                            ShowNr = show.ShowNr,
                            Name = show.Name,
                            Description = show.Description,
                            ReleaseDate = show.ReleaseDate,
                            IsReleasing = show.IsReleasing,
                            AlreadyWatched = show.AlreadyWatched,
                            Image = show.Image
                        },
                        tags = new ObservableCollection<Tag>(tagsForShow)
                    });
                }
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
                AddShowDialog dialog = new AddShowDialog(connection, selected_watchlist, tags, this);
                dialog.ShowDialog();
            }
        }

        private void DeleteWatchlist(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var watchlist_to_delete = button?.DataContext as Watchlist;

            if (watchlist_to_delete != null)
            {
                // Alle Shows in der zu löschenden Watchlist 
                var shows_for_selected_watchlist = connection.GetTable<WatchlistShow>()
                    .Where(ws => ws.WlNr == watchlist_to_delete.WlNr)
                    .Join(connection.GetTable<Show>(),
                        ws => ws.ShowNr,
                        show => show.ShowNr,
                        (ws, show) => show)
                    .ToList();

                // Alle Einträge in der Verbindungstabelle 
                var watchlistshows = connection.GetTable<WatchlistShow>()
                    .Where(ws => ws.WlNr == watchlist_to_delete.WlNr)
                    .ToList();

                // Jeden Watchlist Show Eintrag löschen
                foreach (WatchlistShow watchlistshow in watchlistshows)
                {
                    connection.Delete(watchlistshow);
                }

                // Jede Show in der Watchlist löschen
                foreach (Show show in shows_for_selected_watchlist)
                {
                    var show_tags_to_delete = connection.GetTable<ShowTag>()
                        .Where(st => st.ShowNr == show.ShowNr)
                        .ToList();

                    // Jeden ShowTag Eintrag der Show löschen
                    foreach (ShowTag show_tag in show_tags_to_delete)
                    {
                        connection.Delete(show_tag);
                    }
                    connection.Delete(show);
                }

                // Watchlist selbst löschen
                connection.Delete(watchlist_to_delete);
                watchlists.Remove(watchlist_to_delete);
            }
            else
            {
                MessageBox.Show("[ERROR]: Fehler beim Löschen der Watchlist!");
            }
        }

        private void RemoveShowFromWatchlist(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var showviewmodel = button?.DataContext as ShowViewModel;
            if (showviewmodel != null) 
            {
                var show_to_delete = showviewmodel.show;
                // WatchlistShow, die die zu entfernende Show enthaelt 
                var watchlistshow_with_show_to_delete = connection.GetTable<WatchlistShow>()
                    .FirstOrDefault(ws => ws.ShowNr == show_to_delete.ShowNr);

                // Tags der zu loeschenden Show
                var tag_show_to_delete = connection.GetTable<ShowTag>()
                    .Where(ts => ts.ShowNr == show_to_delete.ShowNr)
                    .ToList();

                if (show_to_delete != null && watchlistshow_with_show_to_delete != null && tag_show_to_delete != null)
                {
                    // Watchlistshow Eintrag loeschen
                    connection.Delete(watchlistshow_with_show_to_delete);

                    // Jeden ShowTag Eintrag loeschen
                    foreach (var tag_show in tag_show_to_delete)
                    {
                        connection.Delete(tag_show);
                    }

                    // Show generell loeschen
                    connection.Delete(show_to_delete);
                    show_view_model.Remove(showviewmodel);
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
            var show_view_model = check_box?.DataContext as ShowViewModel;
            if (show_view_model != null && show_view_model.show != null)
            {
                var show_to_update = show_view_model.show;
                show_to_update.IsReleasing = 1;
                connection.Update(show_to_update);
            }
            return;
        }


        private void IsReleasingUnchecked(object sender, RoutedEventArgs e)
        {
            var check_box = sender as CheckBox;
            var show_view_model = check_box?.DataContext as ShowViewModel;
            if (show_view_model != null && show_view_model.show != null)
            {
                var show_to_update = show_view_model.show;
                show_to_update.IsReleasing = 0;
                connection.Update(show_to_update);
            }
            return;
        }

        private void AlreadyWatchedChecked(object sender, RoutedEventArgs e)
        {
            var check_box = sender as CheckBox;
            var show_view_model = check_box?.DataContext as ShowViewModel;
            if (show_view_model != null && show_view_model.show != null)
            {
                var show_to_update = show_view_model.show;
                show_to_update.AlreadyWatched = 1;
                connection.Update(show_to_update);
            }
            return;
        }

        private void AlreadyWatchedUnchecked(object sender, RoutedEventArgs e)
        {
            var check_box = sender as CheckBox;
            var show_view_model = check_box?.DataContext as ShowViewModel;
            if (show_view_model != null && show_view_model.show != null)
            {
                var show_to_update = show_view_model.show;
                show_to_update.AlreadyWatched = 0;
                connection.Update(show_to_update);
            }
            return;
        }

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
            FilterShows();
        }

        private void FilterShows()
        {
            
        }
    }
}