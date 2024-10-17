using DataModel;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.SQLite;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;

namespace WatchlistApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region Properties
        private readonly WatchlistDatabaseDb connection;
        private Watchlist _selectedWatchlist;
        private string _title, _description, _releaseDate;
        private long _episodes, _stillReleasing, _alreadyWatched;
        private byte[] _imageBytes;
        private BitmapImage _showImage;
        public BitmapImage ShowImage
        {
            get => _showImage;
            set
            {
                _showImage = value;
                OnPropertyChanged(nameof(ShowImage));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        #endregion

        #region Fields
        public List<Tag> filters = new List<Tag>(); // Tags welche gefiltert werden sollen
        private List<Tag> _selectedTags = new List<Tag>();
        public ObservableCollection<WatchlistShow> watchlist_shows { get; set; }
        public ObservableCollection<Tag> tags { get; set; } // Liste an Tags fuer das anzeigen in der UI

        public ObservableCollection<Watchlist> _watchlists;
        public ObservableCollection<Watchlist> watchlists
        {
            get => _watchlists;
            set
            {
                _watchlists = value;
                OnPropertyChanged(nameof(watchlists));
            }
        }

        public ObservableCollection<ShowViewModel> _show_view_models;
        public ObservableCollection<ShowViewModel> show_view_models
        {
            get => _show_view_models;
            set
            {
                _show_view_models = value;
                OnPropertyChanged(nameof(show_view_models));
            }
        }
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

        private void AddShowToWatchlist(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Watchlist selectedWatchlist)
            {
                _selectedWatchlist = selectedWatchlist;
                OpenNewShowPopUp();
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
        private void TagsSelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateSelectedTags();
        private void CancelAddShowToDatabase(object sender, RoutedEventArgs e) => CloseNewShowPopUp();
        private void AddWatchlist(object sender, RoutedEventArgs e) => OpenNewWatchlistPopUp();

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

        private void AddWatchlistToDatabase(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(new_watchlist_field.Text))
            {
                ShowError("Bitte füllen Sie das Namensfeld aus");
                return;
            }
            var newWatchlist = CreateNewWatchlist(new_watchlist_field.Text);
            InsertWatchlistIntoDatabase(newWatchlist);
            CloseNewWatchlistPopUp();
        }

        private void AddShowToDatabase(object sender, RoutedEventArgs e)
        {
            SetVariables();
            if (ValidateInput())
            {
                var insertedShow = InsertShow();
                if (insertedShow != null)
                {
                    InsertShowTags(insertedShow);
                    LinkShowToWatchlist(insertedShow);
                    AddShowToViewModel();
                }
                CloseNewShowPopUp();
            }
            else
            {
                ShowError("Bitte füllen Sie alle Felder aus und wählen Sie ein Bild aus!");
            }
        }

        private void SelectImage(object sender, RoutedEventArgs e)
        {
            var fileDialog = new OpenFileDialog
            {
                DefaultExt = ".png",
                Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp"
            };

            if (fileDialog.ShowDialog() == true)
            {
                _imageBytes = File.ReadAllBytes(fileDialog.FileName);
                ShowImage = new BitmapImage(new Uri(fileDialog.FileName));
            }
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

        private void AddShowToViewModel()
        {
            ShowViewModel showViewModel = new ShowViewModel()
            {
                show = new Show
                {
                    Name = _title,
                    Episodes = _episodes,
                    CurrentEpisode = 0,
                    Description = _description,
                    ReleaseDate = _releaseDate,
                    IsReleasing = _stillReleasing,
                    AlreadyWatched = _alreadyWatched,
                    Image = _imageBytes
                },
                tags = new ObservableCollection<Tag>(_selectedTags)
            };
            show_view_models.Add(showViewModel);
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
                if ((showViewModel.show.CurrentEpisode ?? 0) + addend >= 1)
                {
                    showViewModel.show.CurrentEpisode += addend;
                    connection.Update(showViewModel.show);
                }
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

        private void OpenNewShowPopUp()
        {
            main_content.Effect = new BlurEffect { Radius = 10 };
            addShowPopup.IsOpen = true;
        }

        private void CloseNewShowPopUp()
        {
            addShowPopup.IsOpen = false;
            main_content.Effect = null;
            ClearTextBoxes();
        }

        private void OpenNewWatchlistPopUp()
        {
            main_content.Effect = new BlurEffect { Radius = 10 };
            addWatchlistPopup.IsOpen = true;
        }

        private void CloseNewWatchlistPopUp()
        {
            addWatchlistPopup.IsOpen = false;
            main_content.Effect = null;
            new_watchlist_field.Text = string.Empty;
        }

        private Watchlist CreateNewWatchlist(string watchlistName)
        {
            return new Watchlist
            {
                Name = watchlistName
            };
        }

        private void InsertWatchlistIntoDatabase(Watchlist watchlist)
        {
            connection.Insert(watchlist);
            var insertedWatchlist = GetInsertedWatchlist();
            if (insertedWatchlist != null)
            {
                watchlists.Add(insertedWatchlist);
            }
            else
            {
                ShowError("Fehler beim Hinzufügen der Watchlist");
            }
        }

        private Watchlist GetInsertedWatchlist()
        {
            return connection.GetTable<Watchlist>()
                .OrderByDescending(w => w.WlNr)
                .FirstOrDefault();
        }

        private void SetVariables()
        {
            _title = show_titel_textbox.Text;
            _description = show_name_description_textbox.Text;
            _episodes = long.Parse(show_episodes_textbox.Text);
            _releaseDate = show_release_date_picker.SelectedDate?.ToString("yyyy-MM-dd") ?? string.Empty;
            _stillReleasing = show_still_releasing_checkbox.IsChecked == true ? 1 : 0;
            _alreadyWatched = show_already_watched_checkbox.IsChecked == true ? 1 : 0;
        }

        private bool ValidateInput()
        {
            return !string.IsNullOrEmpty(_title) &&
                   !string.IsNullOrEmpty(_description) &&
                   !string.IsNullOrEmpty(_releaseDate) &&
                   _imageBytes != null;
        }

        private Show InsertShow()
        {
            var show = new Show
            {
                Name = _title,
                Description = _description,
                Episodes = _episodes,
                CurrentEpisode = 0,
                ReleaseDate = _releaseDate,
                IsReleasing = _stillReleasing,
                AlreadyWatched = _alreadyWatched,
                Image = _imageBytes
            };
            connection.Insert(show);

            return connection.GetTable<Show>()
                .OrderByDescending(s => s.ShowNr)
                .FirstOrDefault();
        }

        private void InsertShowTags(Show insertedShow)
        {
            foreach (var tag in _selectedTags)
            {
                var showTag = new ShowTag
                {
                    ShowNr = insertedShow.ShowNr,
                    TagNr = tag.TagNr,
                };
                connection.Insert(showTag);
            }
        }

        private void LinkShowToWatchlist(Show insertedShow)
        {
            var watchlistShow = new WatchlistShow
            {
                WlNr = _selectedWatchlist.WlNr,
                ShowNr = insertedShow.ShowNr
            };
            connection.Insert(watchlistShow);
        }

        private void UpdateSelectedTags()
        {
            _selectedTags.Clear();
            foreach (var item in tags_listbox.SelectedItems)
            {
                if (item is Tag tag)
                {
                    _selectedTags.Add(tag);
                }
            }
        }

        private void ClearTextBoxes()
        {
            show_titel_textbox.Clear();
            show_name_description_textbox.Clear();
            show_episodes_textbox.Clear();
            show_release_date_picker.SelectedDate = null;
            show_still_releasing_checkbox.IsChecked = false;
            show_already_watched_checkbox.IsChecked = false;
            _selectedTags.Clear();
            ShowImage = null;
            tags_listbox.SelectedItems.Clear();
        }

        private void ShowError(string message)
        {
            MessageBox.Show($"[ERROR]: {message}");
        }

        protected virtual void OnPropertyChanged(string property_name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property_name));
        #endregion
    }
}
