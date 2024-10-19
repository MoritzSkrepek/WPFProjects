using DataModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WatchlistApp
{
    public class ShowViewModel : INotifyPropertyChanged
    {
        public Show show {  get; set; }

        public ObservableCollection<Tag> _tags;
        public ObservableCollection<Tag> tags 
        {
            get => _tags;
            set
            {
                _tags = value;
                OnPropertyChanged(nameof(tags));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string property_name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property_name));

        public ShowViewModel()
        {
            tags = new ObservableCollection<Tag>();
        }
    }
}
