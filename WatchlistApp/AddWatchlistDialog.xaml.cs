using DataModel;
using LinqToDB;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;

namespace WatchlistApp
{
    /// <summary>
    /// Interaktionslogik für AddWatchlistDialog.xaml
    /// </summary>
    public partial class AddWatchlistDialog : Window
    {
        private readonly WatchlistDatabaseDb connection;
        private MainWindow main_window;

        public AddWatchlistDialog(WatchlistDatabaseDb c, MainWindow mw)
        {
            connection = c;
            main_window = mw;
            InitializeComponent();
            DataContext = this;
        }

        private void AddWatchlistToDatabase(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(new_watchlist_field.Text))
            {
                Watchlist new_watchlist = new Watchlist()
                {
                    Name = new_watchlist_field.Text
                };
                connection.Insert(new_watchlist);

                // Abrufen der eingefügten Watchlist, um sicherzugehen, dass sie korrekt hinzugefügt wurde
                var insertedWatchlist = connection.GetTable<Watchlist>()
                    .OrderByDescending(w => w.WlNr)
                    .FirstOrDefault();

                if (insertedWatchlist != null)
                {
                    main_window.watchlists.Add(insertedWatchlist);
                    main_window.watchlist_listbox.SelectedItem = insertedWatchlist; // Optional: neue Watchlist auswählen
                    this.Close();
                }
                else
                {
                    MessageBox.Show("[ERROR]: Fehler beim hinzufügen der Watchlist");
                }
            }
            else
            {
                MessageBox.Show("[ERROR]: Bitte füllen Sie das Namensfeld aus");
            }
        }
    }
}
