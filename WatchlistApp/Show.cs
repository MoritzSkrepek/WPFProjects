using LinqToDB.Mapping;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

#nullable enable

namespace DataModel
{
    [Table("Show")]
    public class Show : INotifyPropertyChanged
    {
        private string _name = null!;
        private string _description = null!;
        private string _releaseDate = null!;
        private long _isReleasing;
        private byte[] _image = null!;
        private long _alreadyWatched;
        private long _episodes;
        private long? _currentEpisode;

        [Column("ShowNr", IsPrimaryKey = true, IsIdentity = true, SkipOnInsert = true, SkipOnUpdate = true)]
        public long ShowNr { get; set; }

        [Column("Name", CanBeNull = false)]
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        [Column("Description", CanBeNull = false)]
        public string Description
        {
            get => _description;
            set
            {
                if (_description != value)
                {
                    _description = value;
                    OnPropertyChanged();
                }
            }
        }

        [Column("ReleaseDate", CanBeNull = false)]
        public string ReleaseDate
        {
            get => _releaseDate;
            set
            {
                if (_releaseDate != value)
                {
                    _releaseDate = value;
                    OnPropertyChanged();
                }
            }
        }

        [Column("IsReleasing")]
        public long IsReleasing
        {
            get => _isReleasing;
            set
            {
                if (_isReleasing != value)
                {
                    _isReleasing = value;
                    OnPropertyChanged();
                }
            }
        }

        [Column("Image", CanBeNull = false)]
        public byte[] Image
        {
            get => _image;
            set
            {
                if (_image != value)
                {
                    _image = value;
                    OnPropertyChanged();
                }
            }
        }

        [Column("AlreadyWatched")]
        public long AlreadyWatched
        {
            get => _alreadyWatched;
            set
            {
                if (_alreadyWatched != value)
                {
                    _alreadyWatched = value;
                    OnPropertyChanged();
                }
            }
        }

        [Column("Episodes")]
        public long Episodes
        {
            get => _episodes;
            set
            {
                if (_episodes != value)
                {
                    _episodes = value;
                    OnPropertyChanged();
                }
            }
        }

        [Column("CurrentEpisode")]
        public long? CurrentEpisode
        {
            get => _currentEpisode;
            set
            {
                if (_currentEpisode != value)
                {
                    _currentEpisode = value;
                    OnPropertyChanged();
                }
            }
        }

        #region Associations
        /// <summary>
        /// FK_ShowTag_1_0 backreference
        /// </summary>
        [Association(ThisKey = nameof(ShowNr), OtherKey = nameof(ShowTag.ShowNr))]
        public IEnumerable<ShowTag> ShowTags { get; set; } = null!;

        /// <summary>
        /// FK_WatchlistShow_1_0 backreference
        /// </summary>
        [Association(ThisKey = nameof(ShowNr), OtherKey = nameof(WatchlistShow.ShowNr))]
        public IEnumerable<WatchlistShow> WatchlistShows { get; set; } = null!;
        #endregion

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
