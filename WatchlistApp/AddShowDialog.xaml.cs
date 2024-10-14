using DataModel;
using LinqToDB;
using LinqToDB.SqlQuery;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static LinqToDB.DataProvider.SqlServer.SqlServerProviderAdapter;

namespace WatchlistApp
{
    /// <summary>
    /// Interaktionslogik für AddShowDialog.xaml
    /// </summary>
    public partial class AddShowDialog : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public ObservableCollection<Tag> tags { get; set; }

        private List<Tag> selected_tags = new List<Tag>();
        private readonly WatchlistDatabaseDb connection;
        private Watchlist selected_watchlist;
        private MainWindow main_window;
        private string title;
        private string description;
        private string releasedate;
        private long stillreleasing;
        private long alreadywatched;
        private byte[] image_bytes; 

        private BitmapImage _show_image;
        public BitmapImage show_image
        {
            get { return _show_image; }
            set
            {
                _show_image = value;
                OnPropertyChanged(nameof(show_image));
            }
        }

        public AddShowDialog(WatchlistDatabaseDb c, Watchlist wl, ObservableCollection<Tag> t, MainWindow mw)
        {
            connection = c;
            selected_watchlist = wl;
            main_window = mw;
            tags = t;
            InitializeComponent();
            DataContext = this;
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void SelectImage(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.DefaultExt = ".png";
            fileDialog.Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp";
            if (fileDialog.ShowDialog() == true)
            {
                image_bytes = File.ReadAllBytes(fileDialog.FileName);
                show_image = new BitmapImage(new Uri(fileDialog.FileName));
            }
        }

        private void AddShowToWatchlist(object sender, RoutedEventArgs e)
        {
            SetVariables();
            if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(description) && !string.IsNullOrEmpty(releasedate) && image_bytes != null)
            {
                InsertShow();
                var inserted_show = connection.GetTable<Show>()
                    .OrderByDescending(s => s.ShowNr)
                    .FirstOrDefault();
                if (inserted_show != null)
                {
                    InsertShowTags(inserted_show);  // Tags hinzufügen bevor die UI aktualisiert wird
                    InsertWatchlistShow(inserted_show);
                    UpdateMainWindowUI();
                }
                this.Close();
            }
            else
            {
                MessageBox.Show("[ERROR]: Bitte füllen Sie alle Felder aus und wählen Sie ein Bild aus!");
            }
        }


        private void InsertShowTags(Show inserted_show)
        {
            foreach (Tag tag in selected_tags)
            {
                ShowTag show_tag = new ShowTag()
                {
                    ShowNr = inserted_show.ShowNr,
                    TagNr = tag.TagNr,
                };
                connection.Insert(show_tag);
            }
        }

        private void SetVariables()
        {
            title = show_titel_textblock.Text;
            description = show_name_description_textblock.Text;
            releasedate = show_release_date_picker.SelectedDate?.ToString("yyyy-MM-dd") ?? string.Empty;
            stillreleasing = show_still_releasing_checkbox.IsChecked == true ? 1 : 0;
            alreadywatched = show_already_watched_checkbox.IsChecked == true ? 1 : 0;
        }

        private void UpdateMainWindowUI()
        {
            main_window.show_view_model.Clear();
            var currentShows = connection.GetTable<WatchlistShow>()
                .Where(ws => ws.WlNr == selected_watchlist.WlNr)
                .Join(connection.GetTable<Show>(),
                    ws => ws.ShowNr,
                    show => show.ShowNr,
                    (ws, show) => show)
                .ToList();

            foreach (var show in currentShows)
            {
                var tagsForShow = connection.GetTable<ShowTag>()
                        .Where(st => st.ShowNr == show.ShowNr)
                        .Join(connection.GetTable<Tag>(),
                            st => st.TagNr,
                            tag => tag.TagNr,
                            (st, tag) => tag)
                        .ToList();
                main_window.show_view_model.Add(new ShowViewModel
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

        private void InsertWatchlistShow(Show insertedShow)
        {
            WatchlistShow watchlistShow = new WatchlistShow()
            {
                WlNr = selected_watchlist.WlNr,
                ShowNr = insertedShow.ShowNr
            };

            connection.Insert(watchlistShow);
        }

        private void InsertShow()
        {
            Show show = new Show()
            {
                Name = title,
                Description = description,
                ReleaseDate = releasedate,
                IsReleasing = stillreleasing,
                AlreadyWatched = alreadywatched,
                Image = image_bytes
            };
            connection.Insert(show);
        }

        private void TagsSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selected_tags.Clear();
            foreach (var item in tags_listbox.SelectedItems)
            {
                if (item is Tag tag)
                {
                    selected_tags.Add(tag);
                }
                else
                {
                    return;
                }
            }
        }
    }
}
