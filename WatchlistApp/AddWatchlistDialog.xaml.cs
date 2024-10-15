using DataModel;
using LinqToDB;
using System;
using System.Linq;
using System.Windows;

namespace WatchlistApp
{
    /// <summary>
    /// Interaktionslogik für AddWatchlistDialog.xaml
    /// </summary>
    public partial class AddWatchlistDialog : Window
    {
        #region Fields
        private readonly WatchlistDatabaseDb _databaseConnection;
        private readonly MainWindow _mainWindow;
        #endregion

        #region Constructor
        public AddWatchlistDialog(WatchlistDatabaseDb databaseConnection, MainWindow mainWindow)
        {
            _databaseConnection = databaseConnection;
            _mainWindow = mainWindow;
            InitializeComponent();
            DataContext = this;
        }
        #endregion

        #region Event Handlers
        private void AddWatchlistToDatabase(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(new_watchlist_field.Text))
            {
                ShowError("Bitte füllen Sie das Namensfeld aus");
                return;
            }

            var newWatchlist = CreateNewWatchlist(new_watchlist_field.Text);
            InsertWatchlistIntoDatabase(newWatchlist);
        }
        #endregion

        #region Private Methods
        private Watchlist CreateNewWatchlist(string watchlistName)
        {
            return new Watchlist
            {
                Name = watchlistName
            };
        }

        private void InsertWatchlistIntoDatabase(Watchlist watchlist)
        {
            _databaseConnection.Insert(watchlist);

            var insertedWatchlist = GetInsertedWatchlist();
            if (insertedWatchlist != null)
            {
                AddWatchlistToMainWindow(insertedWatchlist);
                Close();
            }
            else
            {
                ShowError("Fehler beim Hinzufügen der Watchlist");
            }
        }

        private Watchlist GetInsertedWatchlist()
        {
            return _databaseConnection.GetTable<Watchlist>()
                .OrderByDescending(w => w.WlNr)
                .FirstOrDefault();
        }

        private void AddWatchlistToMainWindow(Watchlist watchlist)
        {
            _mainWindow.watchlists.Add(watchlist);
        }

        private void ShowError(string message)
        {
            MessageBox.Show($"[ERROR]: {message}");
        }
        #endregion
    }
}
