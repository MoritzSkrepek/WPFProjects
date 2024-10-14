using DataModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WatchlistApp
{
    public class ShowViewModel
    {
        public Show show {  get; set; }
        public ObservableCollection<Tag> tags { get; set; } = new ObservableCollection<Tag>();
    }
}
