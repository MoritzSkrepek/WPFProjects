using DataModel;
using LinqToDB;
using System;
using System.Collections.Generic;
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

namespace WatchlistApp
{
    /// <summary>
    /// Interaktionslogik für AddWatchlistDialog.xaml
    /// </summary>
    public partial class AddWatchlistDialog : Window
    {
        private readonly WatchlistDatabaseDb connection;
        public Watchlist new_watch_list { get; private set; }

        public AddWatchlistDialog(WatchlistDatabaseDb c)
        {
            connection = c;
            InitializeComponent();
        }

        private void AddWatchlistToDatebase(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(new_watchlist_field.Text))
            {
                new_watch_list = new Watchlist()
                {
                    Name = new_watchlist_field.Text
                };
                connection.Insert(new_watch_list);
                this.DialogResult = true;
                this.Close();
            }
        }
    }
}
