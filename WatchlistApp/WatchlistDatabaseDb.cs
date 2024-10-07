// ---------------------------------------------------------------------------------------------------
// <auto-generated>
// This code was generated by LinqToDB scaffolding tool (https://github.com/linq2db/linq2db).
// Changes to this file may cause incorrect behavior and will be lost if the code is regenerated.
// </auto-generated>
// ---------------------------------------------------------------------------------------------------

using LinqToDB;
using LinqToDB.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable 1573, 1591
#nullable enable

namespace DataModel
{
	public partial class WatchlistDatabaseDb : DataConnection
	{
		public WatchlistDatabaseDb()
		{
			InitDataContext();
		}

		public WatchlistDatabaseDb(string configuration)
			: base(configuration)
		{
			InitDataContext();
		}

		public WatchlistDatabaseDb(DataOptions<WatchlistDatabaseDb> options)
			: base(options.Options)
		{
			InitDataContext();
		}

		partial void InitDataContext();

		public ITable<Show>          Shows          => this.GetTable<Show>();
		public ITable<Watchlist>     Watchlists     => this.GetTable<Watchlist>();
		public ITable<WatchlistShow> WatchlistShows => this.GetTable<WatchlistShow>();
	}

	public static partial class ExtensionMethods
	{
		#region Table Extensions
		public static Show? Find(this ITable<Show> table, long showNr)
		{
			return table.FirstOrDefault(e => e.ShowNr == showNr);
		}

		public static Task<Show?> FindAsync(this ITable<Show> table, long showNr, CancellationToken cancellationToken = default)
		{
			return table.FirstOrDefaultAsync(e => e.ShowNr == showNr, cancellationToken);
		}

		public static Watchlist? Find(this ITable<Watchlist> table, long wlNr)
		{
			return table.FirstOrDefault(e => e.WlNr == wlNr);
		}

		public static Task<Watchlist?> FindAsync(this ITable<Watchlist> table, long wlNr, CancellationToken cancellationToken = default)
		{
			return table.FirstOrDefaultAsync(e => e.WlNr == wlNr, cancellationToken);
		}

		public static WatchlistShow? Find(this ITable<WatchlistShow> table, long wlNr, long showNr)
		{
			return table.FirstOrDefault(e => e.WlNr == wlNr && e.ShowNr == showNr);
		}

		public static Task<WatchlistShow?> FindAsync(this ITable<WatchlistShow> table, long wlNr, long showNr, CancellationToken cancellationToken = default)
		{
			return table.FirstOrDefaultAsync(e => e.WlNr == wlNr && e.ShowNr == showNr, cancellationToken);
		}
		#endregion
	}
}
