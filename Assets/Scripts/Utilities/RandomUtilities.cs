using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Random = UnityEngine.Random;

namespace Utilities
{
    public static class RandomUtilities
    {
        /// <summary>
        /// Returns a random item from the enumerable
        /// </summary>
        /// <param name="enumerable">what we're getting a random item from</param>
        /// <typeparam name="T"></typeparam>
        /// <returns>random element from enumerable</returns>
        /// <exception cref="NullReferenceException">if you give it null</exception>
        public static T RandomElement<T>(IEnumerable<T> enumerable)
        {
            if (enumerable == null)
            {
                throw new NullReferenceException("why are you giving me an empty enumerable???");
            }

            return enumerable.ElementAt(Random.Range(0, enumerable.Count()));

        }
        
        /// <summary>
        /// Shuffles this IList
        /// </summary>
        /// <param name="list"></param>
        /// <typeparam name="T"></typeparam>
        public static void Shuffle<T>(this IList<T> list)
        {

            var size = list.Count;

            for (var i = 0; i < size; i++)
            {
                list.Swap(i, Random.Range(i, size));
            }
        }

        /// <summary>
        /// Creates and shuffles a copy of the given list
        /// </summary>
        /// <param name="theList"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IList<T> ShuffleCopy<T>(this IList<T> theList)
        {
            var newList = new List<T>(theList);
            Shuffle(newList);
            return newList;
        }
 
        /// <summary>
        /// Swaps the items at the given indexes of this list.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <typeparam name="T"></typeparam>
        public static void Swap<T>(this IList<T> list, int i, int j) {
            var temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }
}