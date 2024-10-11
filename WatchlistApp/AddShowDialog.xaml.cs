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
        private readonly WatchlistDatabaseDb connection;
        private Watchlist selected_watchlist;
        private MainWindow main_window;

        public event PropertyChangedEventHandler? PropertyChanged;

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

        public AddShowDialog(WatchlistDatabaseDb c, Watchlist wl, MainWindow mw)
        {
            connection = c;
            selected_watchlist = wl;
            main_window = mw;
            Debug.WriteLine("WlNr von Watchlistshow: " + selected_watchlist.WlNr);
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
            title = show_titel_textblock.Text;
            description = show_name_description_textblock.Text;
            releasedate = show_release_date_picker.SelectedDate?.ToString("yyyy-MM-dd") ?? string.Empty;
            stillreleasing = show_still_releasing_checkbox.IsChecked == true ? 1 : 0;
            alreadywatched = show_already_watched_checkbox.IsChecked == true ? 1 : 0;

            if (!string.IsNullOrEmpty(title) &&
                !string.IsNullOrEmpty(description) &&
                !string.IsNullOrEmpty(releasedate) &&
                image_bytes != null)
            {
                Show show = new Show()
                {
                    Name = title,
                    Description = description,
                    ReleaseDate = releasedate,
                    IsReleasing = stillreleasing,
                    AlreadyWatched = alreadywatched,
                    Image = image_bytes // Muss noch ersetzt werden
                };
                connection.Insert(show);
                var insertedShow = connection.GetTable<Show>()
                    .OrderByDescending(s => s.ShowNr)
                    .FirstOrDefault();
                if (insertedShow != null)
                {
                    WatchlistShow watchlistShow = new WatchlistShow()
                    {
                        WlNr = selected_watchlist.WlNr,
                        ShowNr = insertedShow.ShowNr
                    };
                    Debug.WriteLine(watchlistShow.WlNr);
                    Debug.WriteLine(insertedShow.ShowNr);
                    connection.Insert(watchlistShow);

                    // Shows aus mainwindow fuer die UI updaten
                    var currentShows = connection.GetTable<WatchlistShow>()
                        .Where(ws => ws.WlNr == selected_watchlist.WlNr)
                        .Join(connection.GetTable<Show>(),
                            ws => ws.ShowNr,
                            show => show.ShowNr,
                            (ws, show) => show)
                        .ToList();

                    main_window.shows = new ObservableCollection<Show>(currentShows);
                }
                else
                {
                    MessageBox.Show("Fehler beim Abrufen der Show-ID nach dem Einfügen.");
                }
                this.Close();
            }
            else
            {
                MessageBox.Show("Bitte füllen Sie alle Felder aus und wählen Sie ein Bild aus.");
            }
        }

    }
}
