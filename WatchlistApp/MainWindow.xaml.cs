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
using System.Runtime.ConstrainedExecution;
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
        private ShowViewModel _showViewModel_to_update;
        private string _title, _description, _releaseDate;
        private long _episodes, _stillReleasing, _alreadyWatched;
        private string _edit_title, _edit_description, _edit_releaseDate;
        private long _edit_episodes, _edit_stillReleasing, _edit_alreadyWatched;
        private byte[] _edit_imageBytes;
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

        public ShowViewModel _edit_showviewmodel { get; set; }

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
                ShowError("Fehler beim Löschen der Watchlist!");
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
                ShowError("Fehler beim Löschen der Show!");
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
        private void CancelEditShowToDatabase(object sender, RoutedEventArgs e) => CloseEditPopup();
        private void AddWatchlist(object sender, RoutedEventArgs e) => OpenNewWatchlistPopUp();
        private void editShow(object sender, RoutedEventArgs e) => OpenEditShowPopup(sender, e);
        private void UpdateShowDatabase(object sender, RoutedEventArgs e) => UpdateEditedShow();
        private void RemoveTagFromShow(object sender, RoutedEventArgs e) => RemoveTag(sender, e);
        private void AddTagToShow(object sender, RoutedEventArgs e) => AddTag(sender, e);

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
                ShowError("Bitte wählen Sie eine Watchlist aus.");
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

        private void SelectNewImage(object sender, RoutedEventArgs e)
        {
            var fileDialog = new OpenFileDialog
            {
                DefaultExt = ".png",
                Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp"
            };

            if (fileDialog.ShowDialog() == true)
            {
                _edit_imageBytes = File.ReadAllBytes(fileDialog.FileName);
                ShowImage = new BitmapImage(new Uri(fileDialog.FileName));
                show_edit_image_image.Source = ShowImage;
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

        private void OpenEditShowPopup(object sender, RoutedEventArgs e)
        {
            main_content.Effect = new BlurEffect { Radius = 10 };
            editShowPopup.IsOpen = true;
            LoadShowToEdit(sender, e);
        }

        private void CloseEditPopup()
        {
            editShowPopup.IsOpen = false;
            main_content.Effect = null;
        }

        private void LoadShowToEdit(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is ShowViewModel showViewModel)
            {
                _showViewModel_to_update = showViewModel;
                _edit_showviewmodel = CreateEditShowViewModel(showViewModel);
                editShowPopup.DataContext = _edit_showviewmodel;
                all_tags_listbox.ItemsSource = tags;
            }
        }

        private ShowViewModel CreateEditShowViewModel(ShowViewModel original)
        {
            return new ShowViewModel
            {
                show = new Show
                {
                    ShowNr = original.show.ShowNr,
                    Name = original.show.Name,
                    Image = original.show.Image,
                    Description = original.show.Description,
                    Episodes = original.show.Episodes,
                    CurrentEpisode = original.show.CurrentEpisode,
                    ReleaseDate = original.show.ReleaseDate,
                    IsReleasing = original.show.IsReleasing,
                    AlreadyWatched = original.show.AlreadyWatched,
                },
                tags = new ObservableCollection<Tag>(original.tags)
            };
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

        private void UpdateEditedShow()
        {
            if (!ValidateInputs())
            {
                ShowError("Bitte alle Felder korrekt ausfüllen.");
                return;
            }

            // Updaten der Show UI und DB
            UpdateShowFromUI(_edit_showviewmodel);
            connection.Update(_edit_showviewmodel.show);

            // Updaten der Show UI (tags) und DB
            UpdateTags();
            
            CopyProperties(_edit_showviewmodel, _showViewModel_to_update);
            CloseEditPopup();
        }

        private void UpdateTags()
        {
            var originalTags = _showViewModel_to_update.tags.ToList();
            var editedTags = _edit_showviewmodel.tags.ToList();

            // Hinzugefügte Tags
            var addedTags = editedTags.Except(originalTags).ToList();
            foreach (var tag in addedTags)
            {
                ShowTag showTag = new ShowTag()
                {
                    ShowNr = _edit_showviewmodel.show.ShowNr,
                    TagNr = tag.TagNr
                };
                connection.Insert(showTag);
            }

            // Geloeschte Tags entfernen
            var removedTags = originalTags.Except(editedTags).ToList();
            foreach (var tag in removedTags)
            {
                var showTag = connection.GetTable<ShowTag>()
                    .FirstOrDefault(st => st.ShowNr == _edit_showviewmodel.show.ShowNr && st.TagNr == tag.TagNr);
                if (showTag != null)
                {
                    connection.Delete(showTag);
                }
            }
        }

        private bool ValidateInputs()
        {
            if (string.IsNullOrWhiteSpace(show_edit_titel_textbox.Text) ||
                string.IsNullOrWhiteSpace(show_edit_name_description_textbox.Text) ||
                !long.TryParse(show_edit_episodes_textbox.Text, out _))
            {
                return false;
            }
            return true;
        }

        private void UpdateShowFromUI(ShowViewModel viewModel)
        {
            viewModel.show.Name = show_edit_titel_textbox.Text;
            viewModel.show.Description = show_edit_name_description_textbox.Text;
            viewModel.show.Episodes = long.Parse(show_edit_episodes_textbox.Text);
            viewModel.show.ReleaseDate = show_edit_release_date_picker.SelectedDate?.ToString("yyyy-MM-dd") ?? string.Empty;
            viewModel.show.Image = _edit_imageBytes ?? viewModel.show.Image;
        }

        private void CopyProperties(ShowViewModel source, ShowViewModel target)
        {
            target.show.Name = source.show.Name;
            target.show.Description = source.show.Description;
            target.show.Episodes = source.show.Episodes;
            target.show.ReleaseDate = source.show.ReleaseDate;
            target.show.Image = source.show.Image;
            target.tags = new ObservableCollection<Tag>(source.tags);
        }

        private void RemoveTag(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Tag tag)
            {
                _edit_showviewmodel.tags.Remove(tag);
            }
        }

        private void AddTag(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Tag tag)
            {
                if (!_edit_showviewmodel.tags.Contains(tag))
                {
                    _edit_showviewmodel.tags.Add(tag);
                }
                else
                {
                    ShowError("Show hat diesen Tag bereits.");
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
