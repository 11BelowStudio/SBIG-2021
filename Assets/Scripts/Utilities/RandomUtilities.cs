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
    }
}