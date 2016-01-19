using System;
using System.Collections.Generic;
using System.Linq;

namespace DbMigrations.Client.Infrastructure
{
    public static class Joined
    {
        public static Joined<TKey, TLeft, TRight> Create<TKey, TLeft, TRight>(TKey key, TLeft left, TRight right)
        {
            return new Joined<TKey, TLeft, TRight>(key, left, right);
        }
    }
    public struct Joined<TKey, TLeft, TRight>
    {
        public Joined(TKey key, TLeft left, TRight right)
        {
            Key = key;
            Left = left;
            Right = right;
        }

        public TKey Key { get; }
        public TLeft Left { get; }
        public TRight Right { get; }
    }

    public static class EnumerableExtensions
    {
        public static IEnumerable<Joined<TKey, TLeft, TRight>> FullOuterJoin<TLeft, TRight, TKey>(
            this IEnumerable<TLeft> left,
            IEnumerable<TRight> right,
            Func<TLeft, TKey> leftKeySelector,
            Func<TRight, TKey> rightKeySelector
            )
        {
            var enumeratedLeft = left as IList<TLeft> ?? left.ToList();
            var enumeratedRight = right as IList<TRight> ?? right.ToList();

            var leftJoin = enumeratedLeft.GroupJoin(enumeratedRight,
                leftKeySelector,
                rightKeySelector,
                (l, r) => new Joined<TKey, TLeft, TRight>(leftKeySelector(l), l, r.SingleOrDefault()));

            var rightJoin = enumeratedRight.GroupJoin(enumeratedLeft, rightKeySelector, leftKeySelector,
                (l, r) => new Joined<TKey, TLeft, TRight>(rightKeySelector(l), r.SingleOrDefault(), l));

            return leftJoin.Union(rightJoin);
        }
    }
}