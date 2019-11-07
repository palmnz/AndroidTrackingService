using System;

namespace System.Collections.ObjectModel
{
	public class Pair<_Ty1, _Ty2>
	{
		public _Ty1 First { get; set; }
		public _Ty2 Second { get; set; }

		public Pair() { }

		public Pair( _Ty1 f, _Ty2 s )
		{
			First = f;
			Second = s;
		}
	}

	public class KeyOnlyCollection<TKey> : KeyedCollection<TKey, TKey>
	{
		protected override TKey GetKeyForItem( TKey item )
		{
			return item;
		}
	}
}