using DataModel;
using LinqToDB;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;

namespace WatchlistApp
{
    /// <summary>
    /// Interaktionslogik für AddShowDialog.xaml
    /// </summary>
    public partial class AddShowDialog : Window, INotifyPropertyChanged
    {
        #region Fields
        public event PropertyChangedEventHandler? PropertyChanged;
        public ObservableCollection<Tag> Tags { get; set; }
        private List<Tag> _selectedTags = new List<Tag>();
        private readonly WatchlistDatabaseDb _connection;
        private readonly Watchlist _selectedWatchlist;
        private readonly MainWindow _mainWindow;

        private string _title;
        private string _description;
        private long _episodes;
        private string _releaseDate;
        private long _stillReleasing;
        private long _alreadyWatched;
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
        #endregion

        #region Constructor
        public AddShowDialog(WatchlistDatabaseDb connection, Watchlist watchlist, ObservableCollection<Tag> tags, MainWindow mainWindow)
        {
            _connection = connection;
            _selectedWatchlist = watchlist;
            _mainWindow = mainWindow;
            Tags = tags;
            InitializeComponent();
            DataContext = this;
        }
        #endregion

        #region Event Handlers
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

        private void AddShowToWatchlist(object sender, RoutedEventArgs e)
        {
            SetVariables();
            if (ValidateInput())
            {
                var insertedShow = InsertShow();
                if (insertedShow != null)
                {
                    InsertShowTags(insertedShow);
                    LinkShowToWatchlist(insertedShow);
                    RefreshMainWindowShows();
                }
                Close();
            }
            else
            {
                ShowError("Bitte füllen Sie alle Felder aus und wählen Sie ein Bild aus!");
            }
        }

        private void TagsSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            UpdateSelectedTags();
        }
        #endregion

        #region Private Methods
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
            _connection.Insert(show);

            return _connection.GetTable<Show>()
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
                _connection.Insert(showTag);
            }
        }

        private void LinkShowToWatchlist(Show insertedShow)
        {
            var watchlistShow = new WatchlistShow
            {
                WlNr = _selectedWatchlist.WlNr,
                ShowNr = insertedShow.ShowNr
            };
            _connection.Insert(watchlistShow);
        }

        private void RefreshMainWindowShows()
        {
            _mainWindow.show_view_models.Clear();
            var currentShows = GetCurrentShows();

            foreach (var show in currentShows)
            {
                var tagsForShow = GetTagsForShow(show.ShowNr);
                _mainWindow.show_view_models.Add(new ShowViewModel
                {
                    show = new Show
                    {
                        ShowNr = show.ShowNr,
                        Name = show.Name,
                        Episodes = show.Episodes,
                        CurrentEpisode = 0,
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

        private List<Show> GetCurrentShows()
        {
            return _connection.GetTable<WatchlistShow>()
                .Where(ws => ws.WlNr == _selectedWatchlist.WlNr)
                .Join(_connection.GetTable<Show>(),
                      ws => ws.ShowNr,
                      show => show.ShowNr,
                      (ws, show) => show)
                .ToList();
        }

        private List<Tag> GetTagsForShow(long showNr)
        {
            return _connection.GetTable<ShowTag>()
                .Where(st => st.ShowNr == showNr)
                .Join(_connection.GetTable<Tag>(),
                      st => st.TagNr,
                      tag => tag.TagNr,
                      (st, tag) => tag)
                .ToList();
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

        private void ShowError(string message)
        {
            MessageBox.Show($"[ERROR]: {message}");
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
